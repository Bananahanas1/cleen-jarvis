using System.Diagnostics;

namespace JarvisClean;

// NeuroLinkedBridge — hanterar livscykeln för den Python-baserade brain-servern.
// Per UNIFICATION_PLAN Fas 5: always-on, offline-graceful.
//
// Auto-start: main JarvisForm.OnLoad anropar StartAsync efter dashboard är ready.
// Auto-stop: main FormClosing anropar StopAsync så processen avslutas rent.
//
// Säkerhet:
// - Python-servern binds bara till 127.0.0.1, ingen extern access.
// - Om Python saknas → silent fail med tydlig status-chip i main, ingen krasch.
// - StatusAsync är cachad så status-chips kan polla utan att hamra TCP.
public sealed class NeuroLinkedBridge
{
    private const string PythonRunScript = @"F:\Jarvis-clean\neurolinked\run.py";
    private const string ServerHealthUrl = "http://127.0.0.1:8000/api/state";
    private const int ServerPort = 8000;
    private const int StartupTimeoutSeconds = 12;

    private Process? _serverProcess;
    private string? _resolvedPython;
    private DateTime _startedAt;

    public enum BridgeState { NotStarted, Starting, Ready, Failed, Stopped }
    public BridgeState State { get; private set; } = BridgeState.NotStarted;
    public string? LastError { get; private set; }

    // Försök hitta en fungerande Python-runtime. Samma logik som F:\New project NeuroLinkedBridge:
    // 1. JARVIS_PYTHON env (explicit override)
    // 2. py -3 (Windows launcher, vanligast)
    // 3. python på PATH
    // 4. python3 på PATH
    // Returnerar null om inget fungerar.
    private static string? ResolvePython()
    {
        var envPython = Environment.GetEnvironmentVariable("JARVIS_PYTHON");
        if (!string.IsNullOrWhiteSpace(envPython) && TryRunVersion(envPython))
            return envPython;

        // py -3 på Windows
        if (TryRunVersion("py", "-3 --version"))
            return "py";

        if (TryRunVersion("python"))
            return "python";

        if (TryRunVersion("python3"))
            return "python3";

        return null;
    }

    private static bool TryRunVersion(string exe, string args = "--version")
    {
        try
        {
            var psi = new ProcessStartInfo(exe, args)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var p = Process.Start(psi);
            if (p is null) return false;
            return p.WaitForExit(2000) && p.ExitCode == 0;
        }
        catch { return false; }
    }

    // Startar neurolinked/run.py som child-process. Returnerar tystnad i loggen om Python saknas;
    // main UI visar "Brain: ej tillgänglig" via StatusChipAsync.
    public async Task StartAsync()
    {
        if (State == BridgeState.Ready || State == BridgeState.Starting) return;
        State = BridgeState.Starting;
        LastError = null;

        if (!File.Exists(PythonRunScript))
        {
            State = BridgeState.Failed;
            LastError = "neurolinked/run.py saknas";
            return;
        }

        _resolvedPython = ResolvePython();
        if (_resolvedPython is null)
        {
            State = BridgeState.Failed;
            LastError = "Python ej installerat (provade py -3, python, python3)";
            return;
        }

        try
        {
            // För 'py -3 run.py' måste vi splittra korrekt. För övriga är det bara exe + script.
            var args = _resolvedPython == "py" ? "-3 \"" + PythonRunScript + "\"" : "\"" + PythonRunScript + "\"";
            var psi = new ProcessStartInfo(_resolvedPython, args)
            {
                WorkingDirectory = Path.GetDirectoryName(PythonRunScript)!,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            // Tvinga 127.0.0.1-binding via env-variabel om servern stödjer det.
            psi.Environment["JARVIS_BIND_HOST"] = "127.0.0.1";
            psi.Environment["JARVIS_BIND_PORT"] = ServerPort.ToString();

            _serverProcess = Process.Start(psi);
            if (_serverProcess is null)
            {
                State = BridgeState.Failed;
                LastError = "Process.Start returnerade null";
                return;
            }
            _startedAt = DateTime.Now;

            // Vänta på att servern svarar på /api/state, men blockera inte main för länge.
            var ready = await WaitForReadyAsync(StartupTimeoutSeconds * 1000);
            State = ready ? BridgeState.Ready : BridgeState.Failed;
            if (!ready) LastError = "Server svarade inte inom " + StartupTimeoutSeconds + "s";
        }
        catch (Exception ex)
        {
            State = BridgeState.Failed;
            LastError = ex.Message;
            try { _serverProcess?.Kill(true); } catch { }
            _serverProcess = null;
        }
    }

    private static async Task<bool> WaitForReadyAsync(int totalTimeoutMs)
    {
        var sw = Stopwatch.StartNew();
        using var http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(800) };
        while (sw.ElapsedMilliseconds < totalTimeoutMs)
        {
            try
            {
                var r = await http.GetAsync(ServerHealthUrl);
                if (r.IsSuccessStatusCode) return true;
            }
            catch { /* retry */ }
            await Task.Delay(500);
        }
        return false;
    }

    // Snabb status-koll (ingen HTTP, bara process-state) för UI-chips.
    public string StatusChip()
    {
        return State switch
        {
            BridgeState.Ready => "Brain: redo",
            BridgeState.Starting => "Brain: startar...",
            BridgeState.Failed => "Brain: ej tillgänglig" + (LastError is null ? "" : " (" + LastError + ")"),
            BridgeState.Stopped => "Brain: stoppad",
            _ => "Brain: inte startad"
        };
    }

    // HTTP-baserad live-koll, används av Brain-fönstret för watchdog.
    public async Task<bool> IsAliveAsync()
    {
        if (_serverProcess is null || _serverProcess.HasExited) return false;
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromMilliseconds(800) };
            var r = await http.GetAsync(ServerHealthUrl);
            return r.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task StopAsync()
    {
        try
        {
            if (_serverProcess is not null && !_serverProcess.HasExited)
            {
                _serverProcess.Kill(entireProcessTree: true);
                await _serverProcess.WaitForExitAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }
        }
        catch { }
        finally
        {
            _serverProcess = null;
            State = BridgeState.Stopped;
        }
    }
}
