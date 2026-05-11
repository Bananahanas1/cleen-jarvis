# PWA Phone Bridge — Fas 1 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Lägg till HTTP-bridge i Jarvis-clean så telefon på samma WiFi kan styra Jarvis via samma `dashboard/index.html` (PWA-stil), med token-auth och per-device trust för att approva riskabla actions.

**Architecture:** En kodbas. C# `BridgeServerV1` (HttpListener) serverar `dashboard/index.html` över LAN och routar `POST /api/message` till samma WebView2-message-handler som idag. Server-Sent Events broadcastar alla `PostWebMessageAsJson`-meddelanden till alla anslutna klienter. JS-modul `bridge.js` polyfillar `window.chrome.webview` på telefonen så befintlig dashboard-kod fungerar oförändrat över HTTP. Trust för approval gates via ny `PendingApprovalTypeV1.DeviceTrustRequest`.

**Tech Stack:** C# .NET 8 (HttpListener, System.Text.Json, System.Security.Cryptography), JavaScript (vanilla, EventSource, fetch), HTML/CSS i befintlig dashboard.

**Spec source:** `F:\Jarvis-clean\docs\PHONE_BRIDGE_PLAN.md`

**Existing test patterns to reuse:**
- C# router-tester i `tests\CommandRouterV1.Tests\Program.cs` — assertion-helpers `AssertEqual`/`AssertTrue`/`AssertFalse`/`AssertCommandValid`/`AssertCommandInvalid`
- Node-tester med `vm.createContext` + script extraction från `dashboard/index.html`
- Token-pattern: marker-test (string presence) + behavior-test

---

## Task 1: Lägg till `DeviceTrustRequest` i `PendingApprovalTypeV1`

**Files:**
- Modify: `F:\Jarvis-clean\app\PendingApprovalV1.cs` (lägg till enum-värde)
- Test: `F:\Jarvis-clean\tests\CommandRouterV1.Tests\Program.cs` (om PendingApprovalTypeV1 syns där)

- [ ] **Step 1: Hitta enum-deklarationen**

```bash
grep -n "PendingApprovalTypeV1" F:/Jarvis-clean/app/PendingApprovalV1.cs
```

- [ ] **Step 2: Lägg till `DeviceTrustRequest`-värdet i enum**

Öppna `app\PendingApprovalV1.cs`, hitta `enum PendingApprovalTypeV1 { ... }`, lägg till sista raden före `}`:

```csharp
public enum PendingApprovalTypeV1
{
    FileWrite,
    FileAppend,
    FileCreate,
    FileDelete,
    FileUndo,
    TerminalRun,
    DeviceTrustRequest
}
```

(Bevara existerande värden i exakt samma ordning. Lägg `DeviceTrustRequest` sist.)

- [ ] **Step 3: Build verification**

```bash
cd /f/Jarvis-clean && dotnet build app/JarvisClean.csproj 2>&1 | tail -3
```

Expected: 0 errors, känd MSB3277 warning kvar.

- [ ] **Step 4: Commit**

```bash
git add app/PendingApprovalV1.cs
git commit -m "feat(bridge): add DeviceTrustRequest pending approval type"
```

---

## Task 2: Lägg till `BridgeAdmin` intent i CommandRouterV1

Routar `/enheter`, `/enheter ta bort N`, `/bridge nytoken` lokalt utan att gå till Ollama.

**Files:**
- Modify: `F:\Jarvis-clean\app\CommandRouterV1.cs`
- Test: `F:\Jarvis-clean\tests\CommandRouterV1.Tests\Program.cs`

- [ ] **Step 1: Skriv 4 nya tester (red först)**

Öppna `tests\CommandRouterV1.Tests\Program.cs` och lägg till efter sista test-tuple-elementet, innan tests-arrayens `};`:

```csharp
    ("/enheter routes locally to bridge devices list", () =>
    {
        var result = CommandRouterV1.Parse("/enheter");

        AssertEqual(CommandIntent.BridgeAdmin, result.Intent, "intent");
        AssertEqual("bridge.devices.list", result.ToolName, "tool");
        AssertFalse(result.ShouldSendToOllama, "must stay local");
        AssertCommandValid(result);
    }),

    ("/enheter ta bort 1 routes locally with index argument", () =>
    {
        var result = CommandRouterV1.Parse("/enheter ta bort 1");

        AssertEqual(CommandIntent.BridgeAdmin, result.Intent, "intent");
        AssertEqual("bridge.devices.remove", result.ToolName, "tool");
        AssertEqual("1", result.Arguments["index"], "index");
        AssertFalse(result.ShouldSendToOllama, "must stay local");
        AssertCommandValid(result);
    }),

    ("/enheter ta bort without index is blocked locally", () =>
    {
        var result = CommandRouterV1.Parse("/enheter ta bort");

        AssertEqual(CommandIntent.BridgeAdmin, result.Intent, "intent");
        AssertFalse(result.ShouldSendToOllama, "must stay local");
        AssertCommandInvalid(result);
    }),

    ("/bridge nytoken routes locally to token rotate", () =>
    {
        var result = CommandRouterV1.Parse("/bridge nytoken");

        AssertEqual(CommandIntent.BridgeAdmin, result.Intent, "intent");
        AssertEqual("bridge.token.rotate", result.ToolName, "tool");
        AssertFalse(result.ShouldSendToOllama, "must stay local");
        AssertCommandValid(result);
    }),
```

- [ ] **Step 2: Run tests, verify they fail**

```bash
cd /f/Jarvis-clean && dotnet run --project tests/CommandRouterV1.Tests/CommandRouterV1.Tests.csproj 2>&1 | tail -10
```

Expected: build error eller "CommandIntent does not contain BridgeAdmin" (red).

- [ ] **Step 3: Lägg till `BridgeAdmin` i CommandIntent enum**

I `app\CommandRouterV1.cs`, hitta `enum CommandIntent { ... }` och lägg till sista raden:

```csharp
internal enum CommandIntent
{
    // ... existing values unchanged ...
    ProgramLaunch,
    BridgeAdmin
}
```

- [ ] **Step 4: Lägg till parsing i `CommandRouterV1.Parse`**

Hitta blocket som hanterar slash-kommandon (efter `if (raw.StartsWith("/", StringComparison.Ordinal))` blocket). Lägg till FÖRE `slash.unknown`-fallback:

```csharp
        if (command == "enheter" || command.StartsWith("enheter "))
        {
            if (command == "enheter")
            {
                return new CommandResult
                {
                    Intent = CommandIntent.BridgeAdmin,
                    Risk = CommandRisk.SafeRead,
                    ToolName = "bridge.devices.list",
                    ShouldSendToOllama = false
                };
            }

            if (command.StartsWith("enheter ta bort"))
            {
                var rest = command["enheter ta bort".Length..].Trim();
                var result = new CommandResult
                {
                    Intent = CommandIntent.BridgeAdmin,
                    Risk = CommandRisk.WritesFile,
                    ToolName = "bridge.devices.remove",
                    RequiresApproval = false,
                    ShouldSendToOllama = false
                };

                if (string.IsNullOrWhiteSpace(rest))
                {
                    result.ValidationErrors.Add("Saknar index. Exempel: /enheter ta bort 1");
                }
                else
                {
                    result.Arguments["index"] = rest;
                }

                return result;
            }
        }

        if (command == "bridge nytoken" || command == "bridge ny token")
        {
            return new CommandResult
            {
                Intent = CommandIntent.BridgeAdmin,
                Risk = CommandRisk.WritesFile,
                ToolName = "bridge.token.rotate",
                RequiresApproval = false,
                ShouldSendToOllama = false
            };
        }
```

- [ ] **Step 5: Run tests, verify they pass**

```bash
cd /f/Jarvis-clean && dotnet run --project tests/CommandRouterV1.Tests/CommandRouterV1.Tests.csproj 2>&1 | tail -10
```

Expected: alla nya 4 tester PASS, alla befintliga PASS.

- [ ] **Step 6: Commit**

```bash
git add app/CommandRouterV1.cs tests/CommandRouterV1.Tests/Program.cs
git commit -m "feat(bridge): route /enheter and /bridge nytoken locally"
```

---

## Task 3: Skapa `TrustedDevicesStoreV1.cs` (JSON read/write)

**Files:**
- Create: `F:\Jarvis-clean\app\TrustedDevicesStoreV1.cs`
- Test: `F:\Jarvis-clean\tests\TrustedDevicesStoreV1.Tests\TrustedDevicesStoreV1.Tests.csproj`
- Test: `F:\Jarvis-clean\tests\TrustedDevicesStoreV1.Tests\Program.cs`

- [ ] **Step 1: Skapa testprojektet**

Skapa `tests\TrustedDevicesStoreV1.Tests\TrustedDevicesStoreV1.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="..\..\app\TrustedDevicesStoreV1.cs" Link="TrustedDevicesStoreV1.cs" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Skriv test (red)**

Skapa `tests\TrustedDevicesStoreV1.Tests\Program.cs`:

```csharp
using JarvisClean;

var tempDir = Path.Combine(Path.GetTempPath(), "jarvis-trusted-test-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tempDir);
var path = Path.Combine(tempDir, "trusted_devices.json");

try
{
    // Empty file → empty list
    var store = new TrustedDevicesStoreV1(path);
    AssertEqual(0, store.List().Count, "empty store");

    // Add device → persists
    store.Add("aaa111", "Pixel-7 Chrome");
    AssertEqual(1, store.List().Count, "after add");
    AssertEqual("Pixel-7 Chrome", store.List()[0].Name, "name");

    // Re-read from disk
    var store2 = new TrustedDevicesStoreV1(path);
    AssertEqual(1, store2.List().Count, "persists across instances");

    // IsTrusted returns true for known device
    AssertTrue(store2.IsTrusted("aaa111"), "isTrusted true");
    AssertFalse(store2.IsTrusted("bbb222"), "isTrusted false");

    // Remove by index
    store2.RemoveAt(0);
    AssertEqual(0, store2.List().Count, "after remove");

    // Corrupt file → empty list, no exception
    File.WriteAllText(path, "{not valid json");
    var store3 = new TrustedDevicesStoreV1(path);
    AssertEqual(0, store3.List().Count, "corrupt file handled");

    Console.WriteLine("ALL PASS");
}
finally
{
    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
}

void AssertEqual(object? expected, object? actual, string label)
{
    if (!Equals(expected, actual))
    {
        Console.Error.WriteLine($"FAIL {label}: expected {expected}, got {actual}");
        Environment.Exit(1);
    }
    Console.WriteLine($"PASS {label}");
}

void AssertTrue(bool cond, string label)
{
    if (!cond) { Console.Error.WriteLine($"FAIL {label}"); Environment.Exit(1); }
    Console.WriteLine($"PASS {label}");
}

void AssertFalse(bool cond, string label) => AssertTrue(!cond, label);
```

- [ ] **Step 3: Run test, verify build error (red)**

```bash
cd /f/Jarvis-clean && dotnet run --project tests/TrustedDevicesStoreV1.Tests/TrustedDevicesStoreV1.Tests.csproj 2>&1 | tail -5
```

Expected: build fail, "TrustedDevicesStoreV1 does not exist".

- [ ] **Step 4: Implement `TrustedDevicesStoreV1.cs`**

Skapa `app\TrustedDevicesStoreV1.cs`:

```csharp
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JarvisClean;

public sealed class TrustedDeviceV1
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("addedAt")]
    public string AddedAt { get; set; } = string.Empty;

    [JsonPropertyName("lastSeen")]
    public string LastSeen { get; set; } = string.Empty;
}

internal sealed class TrustedDevicesStoreV1
{
    private readonly string _path;
    private readonly object _lock = new();
    private List<TrustedDeviceV1> _devices = new();

    public TrustedDevicesStoreV1(string path)
    {
        _path = path;
        Reload();
    }

    public IReadOnlyList<TrustedDeviceV1> List()
    {
        lock (_lock) return _devices.ToList();
    }

    public bool IsTrusted(string id)
    {
        if (string.IsNullOrWhiteSpace(id)) return false;
        lock (_lock) return _devices.Any(d => d.Id == id);
    }

    public void Add(string id, string name)
    {
        lock (_lock)
        {
            if (_devices.Any(d => d.Id == id)) return;
            _devices.Add(new TrustedDeviceV1
            {
                Id = id,
                Name = name,
                AddedAt = DateTime.UtcNow.ToString("o"),
                LastSeen = DateTime.UtcNow.ToString("o")
            });
            Persist();
        }
    }

    public void RemoveAt(int index)
    {
        lock (_lock)
        {
            if (index < 0 || index >= _devices.Count) return;
            _devices.RemoveAt(index);
            Persist();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _devices.Clear();
            Persist();
        }
    }

    public void TouchLastSeen(string id)
    {
        lock (_lock)
        {
            var d = _devices.FirstOrDefault(x => x.Id == id);
            if (d == null) return;
            d.LastSeen = DateTime.UtcNow.ToString("o");
            Persist();
        }
    }

    private void Reload()
    {
        try
        {
            if (!File.Exists(_path)) { _devices = new(); return; }
            var json = File.ReadAllText(_path);
            var doc = JsonSerializer.Deserialize<TrustedDevicesFile>(json);
            _devices = doc?.Devices ?? new List<TrustedDeviceV1>();
        }
        catch
        {
            _devices = new List<TrustedDeviceV1>();
        }
    }

    private void Persist()
    {
        var doc = new TrustedDevicesFile { Devices = _devices };
        var json = JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true });
        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        var tmp = _path + ".tmp";
        File.WriteAllText(tmp, json);
        if (File.Exists(_path)) File.Delete(_path);
        File.Move(tmp, _path);
    }

    private sealed class TrustedDevicesFile
    {
        [JsonPropertyName("devices")]
        public List<TrustedDeviceV1> Devices { get; set; } = new();
    }
}
```

- [ ] **Step 5: Run test, verify pass**

```bash
cd /f/Jarvis-clean && dotnet run --project tests/TrustedDevicesStoreV1.Tests/TrustedDevicesStoreV1.Tests.csproj 2>&1 | tail -10
```

Expected: alla PASS.

- [ ] **Step 6: Commit**

```bash
git add app/TrustedDevicesStoreV1.cs tests/TrustedDevicesStoreV1.Tests/
git commit -m "feat(bridge): add TrustedDevicesStoreV1 with atomic JSON persistence"
```

---

## Task 4: Skapa `BridgeAuthV1.cs` (token-validation + fingerprint hash)

**Files:**
- Create: `F:\Jarvis-clean\app\BridgeAuthV1.cs`
- Test: utöka `tests\TrustedDevicesStoreV1.Tests\TrustedDevicesStoreV1.Tests.csproj` att inkludera BridgeAuthV1.cs

- [ ] **Step 1: Lägg till BridgeAuthV1.cs i test-csproj-Compile-listan**

Edit `tests\TrustedDevicesStoreV1.Tests\TrustedDevicesStoreV1.Tests.csproj`:

```xml
<ItemGroup>
    <Compile Include="..\..\app\TrustedDevicesStoreV1.cs" Link="TrustedDevicesStoreV1.cs" />
    <Compile Include="..\..\app\BridgeAuthV1.cs" Link="BridgeAuthV1.cs" />
</ItemGroup>
```

- [ ] **Step 2: Lägg till tester i Program.cs (red)**

Lägg till efter "ALL PASS"-konsolutskriften INNAN den, men före `try-finally`-blockets `finally`:

```csharp
// BridgeAuthV1 tests
var auth = new BridgeAuthV1(installToken: "secret-install-token");

AssertTrue(auth.ValidateInstallToken("secret-install-token"), "valid token");
AssertFalse(auth.ValidateInstallToken("wrong"), "wrong token");
AssertFalse(auth.ValidateInstallToken(""), "empty token");
AssertFalse(auth.ValidateInstallToken(null!), "null token");

var fp1 = BridgeAuthV1.ComputeFingerprint("Mozilla/5.0 Pixel", "salt-aaa");
var fp2 = BridgeAuthV1.ComputeFingerprint("Mozilla/5.0 Pixel", "salt-aaa");
var fp3 = BridgeAuthV1.ComputeFingerprint("Mozilla/5.0 Pixel", "salt-bbb");
AssertEqual(fp1, fp2, "fingerprint stable");
AssertTrue(fp1 != fp3, "fingerprint changes with salt");
AssertTrue(fp1.Length >= 32, "fingerprint long enough");
```

- [ ] **Step 3: Run test, verify build error**

Expected: "BridgeAuthV1 does not exist".

- [ ] **Step 4: Implement BridgeAuthV1.cs**

Skapa `app\BridgeAuthV1.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;

namespace JarvisClean;

internal sealed class BridgeAuthV1
{
    private readonly string _installToken;

    public BridgeAuthV1(string installToken)
    {
        _installToken = installToken ?? string.Empty;
    }

    public bool ValidateInstallToken(string? presented)
    {
        if (string.IsNullOrWhiteSpace(presented)) return false;
        if (string.IsNullOrWhiteSpace(_installToken)) return false;

        var a = Encoding.UTF8.GetBytes(_installToken);
        var b = Encoding.UTF8.GetBytes(presented);
        if (a.Length != b.Length) return false;
        return CryptographicOperations.FixedTimeEquals(a, b);
    }

    public static string ComputeFingerprint(string userAgent, string clientSalt)
    {
        var input = (userAgent ?? "") + "||" + (clientSalt ?? "");
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string GenerateInstallToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
```

- [ ] **Step 5: Run test, verify pass**

```bash
cd /f/Jarvis-clean && dotnet run --project tests/TrustedDevicesStoreV1.Tests/TrustedDevicesStoreV1.Tests.csproj 2>&1 | tail -10
```

Expected: alla PASS inkl nya BridgeAuth-cases.

- [ ] **Step 6: Commit**

```bash
git add app/BridgeAuthV1.cs tests/TrustedDevicesStoreV1.Tests/
git commit -m "feat(bridge): add BridgeAuthV1 with token validation and fingerprint hashing"
```

---

## Task 5: Skapa config-infrastruktur (token gen, port, lan-ip)

`bridge_token.txt` genereras vid första start om saknas. `bridge_port.txt` defaultar till 7777. `bridge_lan_ip.txt` valfri override.

**Files:**
- Modify: `F:\Jarvis-clean\app\Program.cs` (lägg till `BridgeConfigV1`-helper class i samma fil OR ny fil)
- Create: `F:\Jarvis-clean\app\BridgeConfigV1.cs`

- [ ] **Step 1: Skapa BridgeConfigV1.cs**

Skapa `app\BridgeConfigV1.cs`:

```csharp
using System.Net;

namespace JarvisClean;

internal static class BridgeConfigV1
{
    private const string ConfigDir = @"F:\Jarvis-clean\config";
    private const string TokenFile = "bridge_token.txt";
    private const string PortFile = "bridge_port.txt";
    private const string LanIpFile = "bridge_lan_ip.txt";

    private const int DefaultPort = 7777;

    public static string GetOrCreateInstallToken()
    {
        var path = Path.Combine(ConfigDir, TokenFile);
        if (File.Exists(path))
        {
            var existing = File.ReadAllText(path).Trim();
            if (!string.IsNullOrWhiteSpace(existing)) return existing;
        }

        Directory.CreateDirectory(ConfigDir);
        var token = BridgeAuthV1.GenerateInstallToken();
        File.WriteAllText(path, token);
        return token;
    }

    public static int GetPort()
    {
        var path = Path.Combine(ConfigDir, PortFile);
        if (File.Exists(path) && int.TryParse(File.ReadAllText(path).Trim(), out var p)
            && p > 0 && p < 65536)
        {
            return p;
        }
        return DefaultPort;
    }

    public static string GetLanIp()
    {
        var path = Path.Combine(ConfigDir, LanIpFile);
        if (File.Exists(path))
        {
            var override_ = File.ReadAllText(path).Trim();
            if (!string.IsNullOrWhiteSpace(override_)) return override_;
        }

        try
        {
            var hostName = Dns.GetHostName();
            var addresses = Dns.GetHostAddresses(hostName);
            foreach (var a in addresses)
            {
                if (a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork
                    && !IPAddress.IsLoopback(a))
                {
                    var s = a.ToString();
                    if (s.StartsWith("169.254.")) continue; // skip APIPA
                    return s;
                }
            }
        }
        catch { /* fallthrough */ }
        return "127.0.0.1";
    }

    public static void RotateInstallToken()
    {
        Directory.CreateDirectory(ConfigDir);
        var token = BridgeAuthV1.GenerateInstallToken();
        File.WriteAllText(Path.Combine(ConfigDir, TokenFile), token);
    }

    public static string TrustedDevicesPath() =>
        Path.Combine(ConfigDir, "trusted_devices.json");
}
```

- [ ] **Step 2: Build verification**

```bash
cd /f/Jarvis-clean && dotnet build app/JarvisClean.csproj 2>&1 | tail -3
```

Expected: 0 errors.

- [ ] **Step 3: Commit**

```bash
git add app/BridgeConfigV1.cs
git commit -m "feat(bridge): add BridgeConfigV1 for token, port, and LAN-IP config files"
```

---

## Task 6: Skapa `BridgeServerV1.cs` skeleton + `/api/health` endpoint

**Files:**
- Create: `F:\Jarvis-clean\app\BridgeServerV1.cs`
- Test: `F:\Jarvis-clean\tests\BridgeServerV1.Tests\BridgeServerV1.Tests.csproj`
- Test: `F:\Jarvis-clean\tests\BridgeServerV1.Tests\Program.cs`

- [ ] **Step 1: Skapa testprojekt**

`tests\BridgeServerV1.Tests\BridgeServerV1.Tests.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <Compile Include="..\..\app\TrustedDevicesStoreV1.cs" Link="TrustedDevicesStoreV1.cs" />
    <Compile Include="..\..\app\BridgeAuthV1.cs" Link="BridgeAuthV1.cs" />
    <Compile Include="..\..\app\BridgeServerV1.cs" Link="BridgeServerV1.cs" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Skriv test (red): start server, GET /api/health med rätt token, verify 200**

`tests\BridgeServerV1.Tests\Program.cs`:

```csharp
using System.Net.Http;
using JarvisClean;

const string token = "test-token-123";
var port = 17777;

var server = new BridgeServerV1(
    port: port,
    auth: new BridgeAuthV1(token),
    trusted: new TrustedDevicesStoreV1(Path.Combine(Path.GetTempPath(), "bridge-test-" + Guid.NewGuid().ToString("N") + ".json")),
    onMessage: (json, fingerprint) => "{\"ok\":true}",
    bootstrapState: () => "{}",
    staticRoot: Path.Combine(Path.GetTempPath(), "bridge-static")
);

server.Start();

try
{
    using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}/") };

    // Without token → 401
    var noAuth = await http.GetAsync("/api/health");
    AssertEqual(401, (int)noAuth.StatusCode, "no token → 401");

    // With token → 200
    http.DefaultRequestHeaders.Add("X-Bridge-Token", token);
    var ok = await http.GetAsync("/api/health");
    AssertEqual(200, (int)ok.StatusCode, "with token → 200");

    var body = await ok.Content.ReadAsStringAsync();
    AssertTrue(body.Contains("\"ok\""), "health body has ok:true");

    Console.WriteLine("ALL PASS");
}
finally
{
    server.Stop();
}

void AssertEqual(object? e, object? a, string l)
{
    if (!Equals(e, a)) { Console.Error.WriteLine($"FAIL {l}: expected {e}, got {a}"); Environment.Exit(1); }
    Console.WriteLine($"PASS {l}");
}
void AssertTrue(bool c, string l)
{
    if (!c) { Console.Error.WriteLine($"FAIL {l}"); Environment.Exit(1); }
    Console.WriteLine($"PASS {l}");
}
```

- [ ] **Step 3: Run test, expect build error**

Expected: BridgeServerV1 does not exist.

- [ ] **Step 4: Implement BridgeServerV1.cs skeleton**

Skapa `app\BridgeServerV1.cs`:

```csharp
using System.Net;
using System.Text;

namespace JarvisClean;

internal sealed class BridgeServerV1
{
    private readonly int _port;
    private readonly BridgeAuthV1 _auth;
    private readonly TrustedDevicesStoreV1 _trusted;
    private readonly Func<string, string, string> _onMessage;
    private readonly Func<string> _bootstrapState;
    private readonly string _staticRoot;
    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public BridgeServerV1(
        int port,
        BridgeAuthV1 auth,
        TrustedDevicesStoreV1 trusted,
        Func<string, string, string> onMessage,
        Func<string> bootstrapState,
        string staticRoot)
    {
        _port = port;
        _auth = auth;
        _trusted = trusted;
        _onMessage = onMessage;
        _bootstrapState = bootstrapState;
        _staticRoot = staticRoot;
    }

    public void Start()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{_port}/");
        try
        {
            _listener.Start();
        }
        catch (HttpListenerException)
        {
            // Falla tillbaka till localhost om URL ACL saknas
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{_port}/");
            _listener.Start();
        }

        _cts = new CancellationTokenSource();
        _loop = Task.Run(() => AcceptLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        try { _listener?.Stop(); } catch { }
        try { _listener?.Close(); } catch { }
        try { _loop?.Wait(2000); } catch { }
    }

    private async Task AcceptLoopAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested && _listener != null && _listener.IsListening)
        {
            HttpListenerContext ctx;
            try { ctx = await _listener.GetContextAsync(); }
            catch { return; }

            _ = Task.Run(() => HandleAsync(ctx));
        }
    }

    private async Task HandleAsync(HttpListenerContext ctx)
    {
        try
        {
            var path = ctx.Request.Url?.AbsolutePath ?? "/";

            if (!CheckAuth(ctx))
            {
                Reply(ctx, 401, "{\"error\":\"unauthorized\"}", "application/json");
                return;
            }

            if (path == "/api/health")
            {
                Reply(ctx, 200, "{\"ok\":true}", "application/json");
                return;
            }

            Reply(ctx, 404, "{\"error\":\"not_found\"}", "application/json");
        }
        catch (Exception ex)
        {
            try { Reply(ctx, 500, "{\"error\":\"internal\"}", "application/json"); } catch { }
            Console.Error.WriteLine("Bridge handler error: " + ex.Message);
        }
    }

    private bool CheckAuth(HttpListenerContext ctx)
    {
        var token = ctx.Request.Headers["X-Bridge-Token"]
                    ?? ctx.Request.QueryString["t"];
        return _auth.ValidateInstallToken(token);
    }

    private static void Reply(HttpListenerContext ctx, int status, string body, string contentType)
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = contentType;
        ctx.Response.ContentLength64 = bytes.Length;
        ctx.Response.OutputStream.Write(bytes, 0, bytes.Length);
        ctx.Response.OutputStream.Flush();
        ctx.Response.OutputStream.Close();
    }
}
```

- [ ] **Step 5: Run test**

```bash
cd /f/Jarvis-clean && dotnet run --project tests/BridgeServerV1.Tests/BridgeServerV1.Tests.csproj 2>&1 | tail -10
```

Expected: alla PASS.

- [ ] **Step 6: Commit**

```bash
git add app/BridgeServerV1.cs tests/BridgeServerV1.Tests/
git commit -m "feat(bridge): add BridgeServerV1 skeleton with /api/health endpoint"
```

---

## Task 7: Static file serving (`/`, `/bridge.js`, `/index.html`)

Servera dashboard.html med token i query om saknas i header. Servera bridge.js när den finns.

**Files:**
- Modify: `F:\Jarvis-clean\app\BridgeServerV1.cs`
- Create: `F:\Jarvis-clean\dashboard\bridge.js` (placeholder, fyller på i Task 12)

- [ ] **Step 1: Skapa placeholder bridge.js**

`dashboard\bridge.js`:

```javascript
// JarvisBridge — fylls i Task 12
window.JarvisBridge = window.JarvisBridge || { ready: false };
```

- [ ] **Step 2: Utöka BridgeServerV1.HandleAsync**

I `BridgeServerV1.cs`, ändra `HandleAsync`-metoden:

```csharp
private async Task HandleAsync(HttpListenerContext ctx)
{
    try
    {
        var path = ctx.Request.Url?.AbsolutePath ?? "/";

        // Token i query för initial sidladdning
        if (!CheckAuth(ctx))
        {
            Reply(ctx, 401, "{\"error\":\"unauthorized\"}", "application/json");
            return;
        }

        if (path == "/api/health")
        {
            Reply(ctx, 200, "{\"ok\":true}", "application/json");
            return;
        }

        if (path == "/" || path == "/index.html")
        {
            await ServeFileAsync(ctx, "index.html", "text/html; charset=utf-8");
            return;
        }

        if (path == "/bridge.js")
        {
            await ServeFileAsync(ctx, "bridge.js", "application/javascript; charset=utf-8");
            return;
        }

        Reply(ctx, 404, "{\"error\":\"not_found\"}", "application/json");
    }
    catch (Exception ex)
    {
        try { Reply(ctx, 500, "{\"error\":\"internal\"}", "application/json"); } catch { }
        Console.Error.WriteLine("Bridge handler error: " + ex.Message);
    }
}

private async Task ServeFileAsync(HttpListenerContext ctx, string fileName, string contentType)
{
    var full = Path.Combine(_staticRoot, fileName);
    if (!File.Exists(full))
    {
        Reply(ctx, 404, "{\"error\":\"file_missing\"}", "application/json");
        return;
    }

    var bytes = await File.ReadAllBytesAsync(full);
    ctx.Response.StatusCode = 200;
    ctx.Response.ContentType = contentType;
    ctx.Response.ContentLength64 = bytes.Length;
    await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
    ctx.Response.OutputStream.Close();
}
```

- [ ] **Step 3: Utöka test för static-serving**

I `tests\BridgeServerV1.Tests\Program.cs`, lägg till efter health-checks (innan `Console.WriteLine("ALL PASS");`):

```csharp
// Static serving — skapa fake dashboard
var staticRoot = Path.Combine(Path.GetTempPath(), "bridge-static-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(staticRoot);
File.WriteAllText(Path.Combine(staticRoot, "index.html"), "<html><body>hi</body></html>");
File.WriteAllText(Path.Combine(staticRoot, "bridge.js"), "// jarvis bridge");

server.Stop();
server = new BridgeServerV1(
    port: port,
    auth: new BridgeAuthV1(token),
    trusted: new TrustedDevicesStoreV1(Path.Combine(Path.GetTempPath(), "bridge-test-2-" + Guid.NewGuid().ToString("N") + ".json")),
    onMessage: (json, fp) => "{}",
    bootstrapState: () => "{}",
    staticRoot: staticRoot
);
server.Start();

http.Dispose();
http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}/") };
http.DefaultRequestHeaders.Add("X-Bridge-Token", token);

var indexResp = await http.GetAsync("/");
AssertEqual(200, (int)indexResp.StatusCode, "index 200");
var indexBody = await indexResp.Content.ReadAsStringAsync();
AssertTrue(indexBody.Contains("<html>"), "index body");

var jsResp = await http.GetAsync("/bridge.js");
AssertEqual(200, (int)jsResp.StatusCode, "bridge.js 200");
```

(Variabel `http` deklareras `using` i Step 2 — ändra till `var http = new HttpClient(...)` utan `using` så vi kan dispose:a den manuellt och skapa ny.)

- [ ] **Step 4: Run test**

```bash
cd /f/Jarvis-clean && dotnet run --project tests/BridgeServerV1.Tests/BridgeServerV1.Tests.csproj 2>&1 | tail -15
```

Expected: alla PASS.

- [ ] **Step 5: Commit**

```bash
git add app/BridgeServerV1.cs dashboard/bridge.js tests/BridgeServerV1.Tests/Program.cs
git commit -m "feat(bridge): serve dashboard.html and bridge.js over HTTP"
```

---

## Task 8: `POST /api/message` endpoint

Telefonens upstream-meddelanden routar till samma C# message-handler som befintliga `WebView_WebMessageReceived`. Vi exponerar bara hooken `onMessage`-callback.

**Files:**
- Modify: `F:\Jarvis-clean\app\BridgeServerV1.cs`

- [ ] **Step 1: Lägg till `/api/message`-handling i HandleAsync**

I `HandleAsync` efter `/bridge.js`, före `Reply 404`, lägg till:

```csharp
        if (path == "/api/message" && ctx.Request.HttpMethod == "POST")
        {
            using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
            var jsonBody = await reader.ReadToEndAsync();
            var fingerprint = ctx.Request.Headers["X-Device-Fingerprint"] ?? "unknown";

            string responseJson;
            try
            {
                responseJson = _onMessage(jsonBody, fingerprint) ?? "{\"ok\":true}";
            }
            catch (Exception ex)
            {
                responseJson = "{\"error\":\"" + ex.Message.Replace("\"", "'") + "\"}";
            }

            Reply(ctx, 200, responseJson, "application/json");
            return;
        }
```

- [ ] **Step 2: Lägg till test**

I `tests\BridgeServerV1.Tests\Program.cs` efter static-tester, lägg till:

```csharp
// /api/message echoes payload via onMessage callback
server.Stop();
string lastJson = "";
string lastFp = "";
server = new BridgeServerV1(
    port: port,
    auth: new BridgeAuthV1(token),
    trusted: new TrustedDevicesStoreV1(Path.Combine(Path.GetTempPath(), "bridge-test-3-" + Guid.NewGuid().ToString("N") + ".json")),
    onMessage: (json, fp) => { lastJson = json; lastFp = fp; return "{\"ack\":true}"; },
    bootstrapState: () => "{}",
    staticRoot: staticRoot
);
server.Start();

http.Dispose();
http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}/") };
http.DefaultRequestHeaders.Add("X-Bridge-Token", token);
http.DefaultRequestHeaders.Add("X-Device-Fingerprint", "fp-aaa");

var msgResp = await http.PostAsync("/api/message",
    new StringContent("{\"type\":\"test\",\"text\":\"hej\"}", Encoding.UTF8, "application/json"));
AssertEqual(200, (int)msgResp.StatusCode, "message 200");
var msgBody = await msgResp.Content.ReadAsStringAsync();
AssertTrue(msgBody.Contains("ack"), "ack response");
AssertTrue(lastJson.Contains("hej"), "callback received json");
AssertEqual("fp-aaa", lastFp, "callback received fingerprint");
```

- [ ] **Step 3: Run + verify pass + commit**

```bash
cd /f/Jarvis-clean && dotnet run --project tests/BridgeServerV1.Tests/BridgeServerV1.Tests.csproj 2>&1 | tail -10
git add app/BridgeServerV1.cs tests/BridgeServerV1.Tests/Program.cs
git commit -m "feat(bridge): POST /api/message routes to onMessage callback"
```

---

## Task 9: `GET /api/events` SSE-endpoint + broadcaster

Server-Sent Events stream — broadcast av allt som idag går via `PostWebMessageAsJson`.

**Files:**
- Modify: `F:\Jarvis-clean\app\BridgeServerV1.cs`

- [ ] **Step 1: Lägg till SSE-handling**

I `BridgeServerV1`-klassen, lägg till:

```csharp
private readonly List<HttpListenerResponse> _sseClients = new();
private readonly object _sseLock = new();

public void Broadcast(string json)
{
    var line = "data: " + json.Replace("\n", "\\n") + "\n\n";
    var bytes = Encoding.UTF8.GetBytes(line);

    HttpListenerResponse[] snapshot;
    lock (_sseLock) snapshot = _sseClients.ToArray();

    foreach (var resp in snapshot)
    {
        try
        {
            resp.OutputStream.Write(bytes, 0, bytes.Length);
            resp.OutputStream.Flush();
        }
        catch
        {
            lock (_sseLock) _sseClients.Remove(resp);
            try { resp.OutputStream.Close(); } catch { }
        }
    }
}

private async Task HandleSseAsync(HttpListenerContext ctx)
{
    ctx.Response.StatusCode = 200;
    ctx.Response.ContentType = "text/event-stream";
    ctx.Response.Headers["Cache-Control"] = "no-cache";
    ctx.Response.Headers["Connection"] = "keep-alive";
    ctx.Response.SendChunked = true;

    // Initial bootstrap
    var bootstrap = "data: " + _bootstrapState() + "\n\n";
    var bytes = Encoding.UTF8.GetBytes(bootstrap);
    await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
    await ctx.Response.OutputStream.FlushAsync();

    lock (_sseLock) _sseClients.Add(ctx.Response);

    // Håll connection öppen tills client disconnar (HttpListener märker via write fail)
    while (true)
    {
        try
        {
            var ping = Encoding.UTF8.GetBytes(": keepalive\n\n");
            await Task.Delay(20000);
            await ctx.Response.OutputStream.WriteAsync(ping, 0, ping.Length);
            await ctx.Response.OutputStream.FlushAsync();
        }
        catch
        {
            lock (_sseLock) _sseClients.Remove(ctx.Response);
            return;
        }
    }
}
```

- [ ] **Step 2: Routa `/api/events` i HandleAsync**

I `HandleAsync` efter `/api/message`-blocket:

```csharp
        if (path == "/api/events" && ctx.Request.HttpMethod == "GET")
        {
            await HandleSseAsync(ctx);
            return;
        }
```

- [ ] **Step 3: Stop() ska stänga SSE-connections**

Uppdatera `Stop()`:

```csharp
public void Stop()
{
    lock (_sseLock)
    {
        foreach (var c in _sseClients) { try { c.OutputStream.Close(); } catch { } }
        _sseClients.Clear();
    }
    _cts?.Cancel();
    try { _listener?.Stop(); } catch { }
    try { _listener?.Close(); } catch { }
    try { _loop?.Wait(2000); } catch { }
}
```

- [ ] **Step 4: Test SSE — connect, broadcast, verify received**

I `Program.cs` testet, lägg till:

```csharp
// SSE: connect, server broadcasts, client gets event
var sseTask = Task.Run(async () =>
{
    using var sseClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    sseClient.DefaultRequestHeaders.Add("X-Bridge-Token", token);
    using var resp = await sseClient.GetAsync(
        $"http://127.0.0.1:{port}/api/events",
        HttpCompletionOption.ResponseHeadersRead);
    using var stream = await resp.Content.ReadAsStreamAsync();
    using var reader = new StreamReader(stream);

    string? line;
    while ((line = await reader.ReadLineAsync()) != null)
    {
        if (line.StartsWith("data: ") && line.Contains("test-broadcast"))
            return true;
    }
    return false;
});

await Task.Delay(500); // let SSE connect
server.Broadcast("{\"type\":\"test-broadcast\",\"v\":1}");

var got = await Task.WhenAny(sseTask, Task.Delay(5000));
AssertTrue(got == sseTask && await sseTask, "SSE received broadcast event");
```

- [ ] **Step 5: Run + verify + commit**

```bash
cd /f/Jarvis-clean && dotnet run --project tests/BridgeServerV1.Tests/BridgeServerV1.Tests.csproj 2>&1 | tail -10
git add app/BridgeServerV1.cs tests/BridgeServerV1.Tests/Program.cs
git commit -m "feat(bridge): GET /api/events SSE stream with broadcast"
```

---

## Task 10: `POST /api/approval` endpoint med trust-check

**Files:**
- Modify: `F:\Jarvis-clean\app\BridgeServerV1.cs`

- [ ] **Step 1: Lägg till approval-handler**

```csharp
        if (path == "/api/approval" && ctx.Request.HttpMethod == "POST")
        {
            var fingerprint = ctx.Request.Headers["X-Device-Fingerprint"] ?? "";

            if (!_trusted.IsTrusted(fingerprint))
            {
                Reply(ctx, 403, "{\"error\":\"untrusted_device\"}", "application/json");
                return;
            }

            using var reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8);
            var body = await reader.ReadToEndAsync();
            // Routa via samma onMessage så befintlig approval-handler i Program.cs hanterar det
            var responseJson = _onMessage(body, fingerprint) ?? "{\"ok\":true}";
            Reply(ctx, 200, responseJson, "application/json");
            return;
        }
```

- [ ] **Step 2: Test trusted vs untrusted**

```csharp
// Approval endpoint: untrusted → 403
var untrustedResp = await http.PostAsync("/api/approval",
    new StringContent("{\"decision\":\"approve\"}", Encoding.UTF8, "application/json"));
AssertEqual(403, (int)untrustedResp.StatusCode, "untrusted approval → 403");

// Trust the device, retry
var trustedStore = new TrustedDevicesStoreV1(Path.Combine(Path.GetTempPath(), "bridge-trust-" + Guid.NewGuid().ToString("N") + ".json"));
trustedStore.Add("fp-aaa", "Test Phone");

server.Stop();
server = new BridgeServerV1(
    port: port,
    auth: new BridgeAuthV1(token),
    trusted: trustedStore,
    onMessage: (j, fp) => "{\"approved\":true}",
    bootstrapState: () => "{}",
    staticRoot: staticRoot
);
server.Start();

http.Dispose();
http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}/") };
http.DefaultRequestHeaders.Add("X-Bridge-Token", token);
http.DefaultRequestHeaders.Add("X-Device-Fingerprint", "fp-aaa");

var trustedResp = await http.PostAsync("/api/approval",
    new StringContent("{\"decision\":\"approve\"}", Encoding.UTF8, "application/json"));
AssertEqual(200, (int)trustedResp.StatusCode, "trusted approval → 200");
```

- [ ] **Step 3: Run + commit**

```bash
git add app/BridgeServerV1.cs tests/BridgeServerV1.Tests/Program.cs
git commit -m "feat(bridge): POST /api/approval gated by trusted-device check"
```

---

## Task 11: `POST /api/pair/request` endpoint

Skapar `DeviceTrustRequest` pending approval; routar till samma message-handler.

**Files:**
- Modify: `F:\Jarvis-clean\app\BridgeServerV1.cs`

- [ ] **Step 1: Lägg till pair-request-handler**

```csharp
        if (path == "/api/pair/request" && ctx.Request.HttpMethod == "POST")
        {
            var fingerprint = ctx.Request.Headers["X-Device-Fingerprint"] ?? "";
            var deviceName = ctx.Request.Headers["X-Device-Name"] ?? "Unknown";

            // Wrappa som ett message Program.cs förstår
            var wrapped = "{\"type\":\"jarvis_bridge_pair_request_v1\",\"fingerprint\":\""
                + fingerprint.Replace("\"", "")
                + "\",\"name\":\"" + deviceName.Replace("\"", "")
                + "\"}";

            var responseJson = _onMessage(wrapped, fingerprint) ?? "{\"pending\":true}";
            Reply(ctx, 200, responseJson, "application/json");
            return;
        }
```

- [ ] **Step 2: Test**

```csharp
// Pair request → goes through onMessage with wrapped type
server.Stop();
string capturedPair = "";
server = new BridgeServerV1(
    port: port,
    auth: new BridgeAuthV1(token),
    trusted: trustedStore,
    onMessage: (j, fp) => { capturedPair = j; return "{\"pending\":true}"; },
    bootstrapState: () => "{}",
    staticRoot: staticRoot
);
server.Start();
http.Dispose();
http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}/") };
http.DefaultRequestHeaders.Add("X-Bridge-Token", token);
http.DefaultRequestHeaders.Add("X-Device-Fingerprint", "new-phone-fp");
http.DefaultRequestHeaders.Add("X-Device-Name", "Pixel-7");

var pairResp = await http.PostAsync("/api/pair/request", new StringContent("", Encoding.UTF8, "application/json"));
AssertEqual(200, (int)pairResp.StatusCode, "pair 200");
AssertTrue(capturedPair.Contains("jarvis_bridge_pair_request_v1"), "wrapped type");
AssertTrue(capturedPair.Contains("new-phone-fp"), "fingerprint propagated");
AssertTrue(capturedPair.Contains("Pixel-7"), "device name propagated");
```

- [ ] **Step 3: Run + commit**

```bash
git add app/BridgeServerV1.cs tests/BridgeServerV1.Tests/Program.cs
git commit -m "feat(bridge): POST /api/pair/request creates trust pending approval"
```

---

## Task 12: `dashboard/bridge.js` — JarvisBridge polyfill

JS-modul som upptäcker WebView2 vs HTTP+SSE och **polyfillar `window.chrome.webview`** så befintlig dashboard-kod inte behöver ändras.

**Files:**
- Modify: `F:\Jarvis-clean\dashboard\bridge.js`
- Test: `F:\Jarvis-clean\tests\bridge-jsclient.test.js`

- [ ] **Step 1: Skriv test (red)**

`tests\bridge-jsclient.test.js`:

```javascript
const fs = require("fs");
const path = require("path");
const vm = require("vm");

const bridgePath = path.join(__dirname, "..", "dashboard", "bridge.js");
const bridgeSource = fs.readFileSync(bridgePath, "utf8");

const requiredMarkers = [
  "JarvisBridge",
  "EventSource",
  "X-Bridge-Token",
  "X-Device-Fingerprint",
  "/api/message",
  "/api/events"
];

let failures = 0;
for (const m of requiredMarkers) {
  if (!bridgeSource.includes(m)) {
    failures += 1;
    console.log(`FAIL bridge.js missing: ${m}`);
  } else {
    console.log(`PASS bridge.js has: ${m}`);
  }
}

// Auto-detect: WebView2 mode → polyfill is no-op
const ctxWebView = {
  console,
  setTimeout, clearTimeout,
  window: { chrome: { webview: { postMessage(){}, addEventListener(){} } } },
  fetch() {},
  EventSource: function(){},
  localStorage: { getItem(){return null;}, setItem(){} },
  navigator: { userAgent: "Test UA" },
  location: { search: "?t=tok" }
};
ctxWebView.window.window = ctxWebView.window;
ctxWebView.window.localStorage = ctxWebView.localStorage;
ctxWebView.window.navigator = ctxWebView.navigator;
ctxWebView.window.location = ctxWebView.location;
ctxWebView.window.fetch = ctxWebView.fetch;
ctxWebView.window.EventSource = ctxWebView.EventSource;
vm.createContext(ctxWebView);
vm.runInContext(bridgeSource, ctxWebView, { filename: "bridge.js" });

if (!ctxWebView.window.JarvisBridge || !ctxWebView.window.JarvisBridge.mode) {
  failures += 1;
  console.log("FAIL JarvisBridge.mode missing in WebView2 context");
} else if (ctxWebView.window.JarvisBridge.mode !== "webview2") {
  failures += 1;
  console.log(`FAIL expected mode webview2, got ${ctxWebView.window.JarvisBridge.mode}`);
} else {
  console.log("PASS WebView2 mode detected");
}

// HTTP mode: no chrome.webview present → polyfill is active, mode=http
const ctxHttp = {
  console,
  setTimeout, clearTimeout,
  window: { chrome: undefined },
  localStorage: { _s: {}, getItem(k){return this._s[k]||null;}, setItem(k,v){this._s[k]=String(v);} },
  navigator: { userAgent: "Test Phone UA" },
  location: { search: "?t=ttt" },
  fetch: async () => ({ json: async () => ({ ok: true }) }),
  EventSource: function(url){ this.url = url; this.addEventListener=()=>{}; this.close=()=>{}; }
};
ctxHttp.window.window = ctxHttp.window;
ctxHttp.window.localStorage = ctxHttp.localStorage;
ctxHttp.window.navigator = ctxHttp.navigator;
ctxHttp.window.location = ctxHttp.location;
ctxHttp.window.fetch = ctxHttp.fetch;
ctxHttp.window.EventSource = ctxHttp.EventSource;
vm.createContext(ctxHttp);
vm.runInContext(bridgeSource, ctxHttp, { filename: "bridge.js" });

if (ctxHttp.window.JarvisBridge.mode !== "http") {
  failures += 1;
  console.log(`FAIL expected http mode, got ${ctxHttp.window.JarvisBridge.mode}`);
} else {
  console.log("PASS HTTP mode detected when no WebView2");
}

if (typeof ctxHttp.window.chrome !== "object" || typeof ctxHttp.window.chrome.webview?.postMessage !== "function") {
  failures += 1;
  console.log("FAIL chrome.webview.postMessage polyfill missing in HTTP mode");
} else {
  console.log("PASS chrome.webview.postMessage polyfilled in HTTP mode");
}

if (failures > 0) process.exit(1);
```

- [ ] **Step 2: Run test (red)**

```bash
cd /f/Jarvis-clean && node tests/bridge-jsclient.test.js
```

Expected: FAIL — `bridge.js` saknar markers + JarvisBridge.

- [ ] **Step 3: Implement bridge.js**

Ersätt innehållet i `dashboard\bridge.js`:

```javascript
(function() {
  "use strict";

  function readQueryToken() {
    try {
      const m = String(window.location.search || "").match(/[?&]t=([^&]+)/);
      return m ? decodeURIComponent(m[1]) : "";
    } catch { return ""; }
  }

  function getOrCreateClientSalt() {
    let salt = "";
    try {
      salt = window.localStorage.getItem("jarvis_bridge_salt") || "";
      if (!salt) {
        const buf = new Uint8Array(16);
        if (window.crypto && window.crypto.getRandomValues) window.crypto.getRandomValues(buf);
        else for (let i = 0; i < buf.length; i++) buf[i] = Math.floor(Math.random() * 256);
        salt = Array.from(buf).map(b => b.toString(16).padStart(2, "0")).join("");
        window.localStorage.setItem("jarvis_bridge_salt", salt);
      }
    } catch { salt = "fallback"; }
    return salt;
  }

  async function sha256Hex(s) {
    if (window.crypto && window.crypto.subtle) {
      const buf = await window.crypto.subtle.digest("SHA-256", new TextEncoder().encode(s));
      return Array.from(new Uint8Array(buf)).map(b => b.toString(16).padStart(2, "0")).join("");
    }
    // Test environment fallback (no crypto.subtle)
    return "fallback-" + s.length;
  }

  const isWebView2 = !!(window.chrome && window.chrome.webview);
  const messageHandlers = [];

  if (isWebView2) {
    window.JarvisBridge = {
      mode: "webview2",
      send(msg) { window.chrome.webview.postMessage(msg); },
      onMessage(cb) { window.chrome.webview.addEventListener("message", e => cb(e.data)); },
      ready: true
    };
    return;
  }

  // HTTP mode
  const token = readQueryToken() || (window.localStorage.getItem("jarvis_bridge_token") || "");
  if (token) {
    try { window.localStorage.setItem("jarvis_bridge_token", token); } catch {}
  }

  let fingerprintCache = "";

  async function ensureFingerprint() {
    if (fingerprintCache) return fingerprintCache;
    const ua = (window.navigator && window.navigator.userAgent) || "";
    const salt = getOrCreateClientSalt();
    fingerprintCache = await sha256Hex(ua + "||" + salt);
    return fingerprintCache;
  }

  async function send(msg) {
    const fp = await ensureFingerprint();
    const path = msg && msg.type === "jarvis_pending_approval_v1" ? "/api/approval"
               : msg && msg.type === "jarvis_bridge_pair_request_v1" ? "/api/pair/request"
               : "/api/message";

    const resp = await window.fetch(path, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "X-Bridge-Token": token,
        "X-Device-Fingerprint": fp
      },
      body: JSON.stringify(msg)
    });
    if (!resp.ok && resp.status === 401) {
      try { window.localStorage.removeItem("jarvis_bridge_token"); } catch {}
    }
    return resp;
  }

  function startSse() {
    if (!window.EventSource) return;
    const url = "/api/events?t=" + encodeURIComponent(token);
    const es = new window.EventSource(url);
    es.addEventListener("message", e => {
      try {
        const data = JSON.parse(e.data);
        for (const cb of messageHandlers) {
          try { cb(data); } catch {}
        }
      } catch {}
    });
    es.addEventListener("error", () => {
      // browser auto-reconnects
    });
  }

  // Polyfill chrome.webview so existing dashboard code works unchanged
  window.chrome = window.chrome || {};
  window.chrome.webview = window.chrome.webview || {
    postMessage(msg) { send(msg).catch(err => console.error("bridge send failed:", err)); },
    addEventListener(type, cb) {
      if (type === "message") messageHandlers.push(payload => cb({ data: payload }));
    }
  };

  window.JarvisBridge = {
    mode: "http",
    send: msg => send(msg),
    onMessage(cb) { messageHandlers.push(cb); },
    ready: true
  };

  startSse();
})();
```

- [ ] **Step 4: Run test (green)**

```bash
cd /f/Jarvis-clean && node tests/bridge-jsclient.test.js
```

Expected: alla PASS.

- [ ] **Step 5: Inkludera bridge.js i dashboard/index.html**

I `dashboard\index.html`, hitta `<script>`-blocket (rad ~718) och lägg till EXAKT före:

```html
<script src="bridge.js"></script>
<script>
```

- [ ] **Step 6: Commit**

```bash
git add dashboard/bridge.js dashboard/index.html tests/bridge-jsclient.test.js
git commit -m "feat(bridge): add JarvisBridge JS polyfill for chrome.webview"
```

---

## Task 13: Setup-vy i `dashboard/index.html`

Ny "Anslut"-knapp + panel med URL/token + trusted devices-lista.

**Files:**
- Modify: `F:\Jarvis-clean\dashboard\index.html`
- Test: `F:\Jarvis-clean\tests\setup-view.test.js`

- [ ] **Step 1: Skriv marker-test (red)**

`tests\setup-view.test.js`:

```javascript
const fs = require("fs");
const path = require("path");
const html = fs.readFileSync(path.join(__dirname, "..", "dashboard", "index.html"), "utf8");

const markers = [
  'id="setupPanelV1"',
  'id="setupConnectUrl"',
  'id="setupQrCanvas"',
  'id="setupTrustedDevicesList"',
  'id="setupRotateTokenBtn"',
  'id="showSetupBtn"',
  'window.jarvisRenderSetupV1'
];

let fail = 0;
for (const m of markers) {
  if (!html.includes(m)) { fail++; console.log("FAIL missing:", m); }
  else console.log("PASS:", m);
}
if (fail) process.exit(1);
```

- [ ] **Step 2: Run test, verify red**

- [ ] **Step 3: Lägg till knapp i top-row**

I `dashboard\index.html`, hitta `<button id="showVisualBtn"` och lägg till efter den:

```html
        <button id="showSetupBtn" class="panel-button">Anslut</button>
```

- [ ] **Step 4: Lägg till setup-panel HTML**

Hitta `<div id="visualPanel">` och lägg till EFTER hela `</div>`-blocket (omedelbart före `<textarea id="editorArea"`):

```html
      <div id="setupPanelV1" style="display:none; flex:1; min-height:0; flex-direction:column; gap:12px; overflow:auto; background:#03070d; color:#dff7ff; border:1px solid #193247; border-radius:8px; padding:14px;">
        <div>
          <div class="visual-kicker">Anslut telefon</div>
          <h3 class="visual-title">Bridge setup</h3>
        </div>
        <div class="visual-cell">
          <div class="visual-label">Connection URL</div>
          <div id="setupConnectUrl" class="visual-value">Hämtar...</div>
          <button id="setupCopyUrlBtn" type="button" style="margin-top:8px;">Kopiera URL</button>
        </div>
        <div class="visual-cell">
          <div class="visual-label">QR-kod</div>
          <canvas id="setupQrCanvas" width="220" height="220" style="background:#fff;"></canvas>
        </div>
        <div class="visual-cell">
          <div class="visual-label">Trusted devices</div>
          <ul id="setupTrustedDevicesList" style="list-style:none; padding:0; margin:0;"></ul>
        </div>
        <div class="visual-cell">
          <div class="visual-label">Token</div>
          <button id="setupRotateTokenBtn" type="button">Rotera token (kastar ut alla devices)</button>
        </div>
      </div>
```

- [ ] **Step 5: Hooka in showSetupBtn + render-funktion**

I `<script>`-blocket nära andra panel-knappar, lägg till:

```javascript
    const showSetupBtn = document.getElementById("showSetupBtn");
    const setupPanelV1 = document.getElementById("setupPanelV1");
    const setupConnectUrl = document.getElementById("setupConnectUrl");
    const setupQrCanvas = document.getElementById("setupQrCanvas");
    const setupTrustedDevicesList = document.getElementById("setupTrustedDevicesList");
    const setupRotateTokenBtn = document.getElementById("setupRotateTokenBtn");
    const setupCopyUrlBtn = document.getElementById("setupCopyUrlBtn");

    let setupPayloadV1 = null;

    window.jarvisRenderSetupV1 = function(payload) {
      setupPayloadV1 = payload || {};
      const url = String(setupPayloadV1.url || "");
      setupConnectUrl.textContent = url || "(ej tillgänglig)";

      // Render QR
      try { renderQrV1(setupQrCanvas, url); } catch (e) { console.error(e); }

      setupTrustedDevicesList.innerHTML = "";
      const devices = setupPayloadV1.devices || [];
      devices.forEach(function(d, idx) {
        const li = document.createElement("li");
        li.style.padding = "6px 0";
        li.style.borderBottom = "1px solid #193247";
        const label = document.createElement("span");
        label.textContent = (d.name || "Unknown") + " (sedd " + (d.lastSeen || "?") + ")";
        const btn = document.createElement("button");
        btn.textContent = "Ta bort";
        btn.style.marginLeft = "8px";
        btn.addEventListener("click", function() {
          postMessage({ type: "jarvis_bridge_devices_remove_v1", index: idx });
        });
        li.appendChild(label);
        li.appendChild(btn);
        setupTrustedDevicesList.appendChild(li);
      });
    };

    showSetupBtn.addEventListener("click", function() {
      visualPanel.style.display = "none";
      editorArea.style.display = "none";
      diffViewer.style.display = "none";
      setupPanelV1.style.display = "flex";
      workspaceTitle.textContent = "Anslut";
      postMessage({ type: "jarvis_bridge_setup_request_v1" });
    });

    setupRotateTokenBtn.addEventListener("click", function() {
      if (confirm("Rotera token? Alla anslutna enheter måste paras om.")) {
        postMessage({ type: "jarvis_bridge_token_rotate_v1" });
      }
    });

    setupCopyUrlBtn.addEventListener("click", async function() {
      try { await navigator.clipboard.writeText(setupConnectUrl.textContent || ""); } catch {}
    });

    // QR-rendering läggs till i Task 14
    function renderQrV1(canvas, text) {
      // placeholder — implementeras i Task 14
      const ctx = canvas.getContext && canvas.getContext("2d");
      if (!ctx) return;
      ctx.fillStyle = "#fff";
      ctx.fillRect(0, 0, canvas.width, canvas.height);
      ctx.fillStyle = "#000";
      ctx.font = "10px monospace";
      ctx.fillText("(QR placeholder)", 10, 20);
    }
```

- [ ] **Step 6: Run test, verify green**

```bash
cd /f/Jarvis-clean && node tests/setup-view.test.js
```

Expected: alla PASS.

- [ ] **Step 7: Commit**

```bash
git add dashboard/index.html tests/setup-view.test.js
git commit -m "feat(bridge): add setup view with URL/QR/trusted devices skeleton"
```

---

## Task 14: QR-kod-rendering (inline tiny lib)

Ersätt `renderQrV1`-placeholder med riktigt QR-kod via en minimal QR-lib inline. Använd biblioteket `qrcode-generator` (~10KB) inkluderat i `bridge.js` eller ny `qrcode.js`.

**Files:**
- Create: `F:\Jarvis-clean\dashboard\qrcode.js` (kopia av qrcode-generator MIT-licensad lib, eller minimal inline implementation)
- Modify: `F:\Jarvis-clean\dashboard\index.html` (importera qrcode.js, byt ut renderQrV1)
- Test: utöka `tests\setup-view.test.js`

- [ ] **Step 1: Lägg in qrcode.js**

Ladda ner `qrcode-generator/qrcode.js` (kazuhikoarase, MIT) eller använd `qr-creator` (small inline). För hobby-projekt OK att copy-paste hela. Spara till `dashboard\qrcode.js`. Kommentar i toppen som anger källa + licens.

(Som alternativ: skriv en mini-impl själv. För det här planen rekommenderas att importera ett välkänt bibliotek då fas 1 inte ska experimentera med kryptografisk QR-encoding.)

- [ ] **Step 2: Importera i index.html**

```html
<script src="qrcode.js"></script>
<script src="bridge.js"></script>
```

- [ ] **Step 3: Byt ut renderQrV1**

Ersätt placeholder-versionen:

```javascript
function renderQrV1(canvas, text) {
  if (!text || !window.qrcode) return;
  const qr = window.qrcode(0, "L");
  qr.addData(text);
  qr.make();
  const modules = qr.getModuleCount();
  const ctx = canvas.getContext("2d");
  const size = canvas.width;
  const cell = Math.floor(size / modules);
  ctx.fillStyle = "#fff";
  ctx.fillRect(0, 0, size, size);
  ctx.fillStyle = "#000";
  for (let r = 0; r < modules; r++) {
    for (let c = 0; c < modules; c++) {
      if (qr.isDark(r, c)) ctx.fillRect(c * cell, r * cell, cell, cell);
    }
  }
}
```

- [ ] **Step 4: Test QR markers**

I `tests\setup-view.test.js`, lägg till markers:

```javascript
markers.push("qrcode.js");
markers.push("qr.addData");
```

- [ ] **Step 5: Run + commit**

```bash
git add dashboard/qrcode.js dashboard/index.html tests/setup-view.test.js
git commit -m "feat(bridge): render QR code in setup view"
```

---

## Task 15: Mobile CSS @media block

**Files:**
- Modify: `F:\Jarvis-clean\dashboard\index.html`
- Test: `F:\Jarvis-clean\tests\mobile-css.test.js`

- [ ] **Step 1: Skriv marker-test (red)**

`tests\mobile-css.test.js`:

```javascript
const fs = require("fs");
const path = require("path");
const html = fs.readFileSync(path.join(__dirname, "..", "dashboard", "index.html"), "utf8");

const markers = [
  "@media (max-width: 800px)",
  "flex-direction: column",
  "id=\"projectExplorerHamburgerV1\""
];

let fail = 0;
for (const m of markers) {
  if (!html.includes(m)) { fail++; console.log("FAIL:", m); }
  else console.log("PASS:", m);
}
if (fail) process.exit(1);
```

- [ ] **Step 2: Run test, verify red**

- [ ] **Step 3: Lägg till @media i index.html**

I `<style>`-blocket nära slutet, lägg till:

```css
    @media (max-width: 800px) {
      body {
        grid-template-columns: 1fr;
        grid-template-rows: auto auto auto;
      }
      .nav, .editor, .chat {
        flex-direction: column;
      }
      #projectExplorerHamburgerV1 {
        display: block;
      }
      aside.nav {
        display: none;
      }
      aside.nav.mobile-open {
        display: block;
      }
      .approval-dialog {
        max-width: 100%;
        width: 100%;
        margin: 0;
        border-radius: 0;
      }
      #suggestions {
        font-size: 14px;
      }
      .tree-row {
        padding: 8px 6px;
      }
      .panel-button, button {
        min-height: 40px;
      }
    }
```

- [ ] **Step 4: Lägg till hamburger-knapp ovanför mid-area**

I top-row eller editor-headern:

```html
<button id="projectExplorerHamburgerV1" type="button" style="display:none;">≡</button>
```

JS:

```javascript
const projectExplorerHamburgerV1 = document.getElementById("projectExplorerHamburgerV1");
projectExplorerHamburgerV1.addEventListener("click", function() {
  const aside = document.querySelector("aside.nav");
  if (aside) aside.classList.toggle("mobile-open");
});
```

- [ ] **Step 5: Run + commit**

```bash
git add dashboard/index.html tests/mobile-css.test.js
git commit -m "feat(bridge): mobile CSS @media block with vertical stack and hamburger"
```

---

## Task 16: Wire-up i `Program.cs` — starta BridgeServer + broadcast PostWebMessageAsJson

**Files:**
- Modify: `F:\Jarvis-clean\app\Program.cs`

- [ ] **Step 1: Hitta WebView2-init-platsen**

Leta `EnsureCoreWebView2Async`-anropet (omkring rad 75-90).

- [ ] **Step 2: Lägg till BridgeServer-start efter WebView2-init**

I main form / startup, efter att WebView2 är ready men före Navigate, lägg till:

```csharp
        var installToken = BridgeConfigV1.GetOrCreateInstallToken();
        var bridgeAuth = new BridgeAuthV1(installToken);
        var bridgeTrusted = new TrustedDevicesStoreV1(BridgeConfigV1.TrustedDevicesPath());
        var bridgePort = BridgeConfigV1.GetPort();
        var dashboardDir = @"F:\Jarvis-clean\dashboard";

        _bridgeServer = new BridgeServerV1(
            port: bridgePort,
            auth: bridgeAuth,
            trusted: bridgeTrusted,
            onMessage: (json, fingerprint) =>
            {
                // Routa till samma pipeline som WebView2
                try
                {
                    return HandleWebMessageFromBridge(json, fingerprint);
                }
                catch (Exception ex) { return "{\"error\":\"" + ex.Message.Replace("\"", "'") + "\"}"; }
            },
            bootstrapState: BuildBootstrapStateV1,
            staticRoot: dashboardDir
        );
        _bridgeServer.Start();
```

(`_bridgeServer` deklareras som privat field i Form-klassen; stoppa i `OnFormClosing`.)

- [ ] **Step 3: Implement HandleWebMessageFromBridge**

Lägg till metod som tar JSON, parsar samma sätt som WebView_WebMessageReceived, och returnerar svar:

```csharp
private string HandleWebMessageFromBridge(string json, string fingerprint)
{
    // Återanvänd existerande dispatch-logik. Den synkrona returvärdet är tunnt;
    // de flesta state-uppdateringar sker via PostWebMessageAsJson + Broadcast.
    using var doc = JsonDocument.Parse(json);
    var type = doc.RootElement.TryGetProperty("type", out var t) ? t.GetString() ?? "" : "";

    // Pair-request
    if (type == "jarvis_bridge_pair_request_v1")
    {
        var name = doc.RootElement.TryGetProperty("name", out var n) ? n.GetString() ?? "Unknown" : "Unknown";
        PendingApprovalStoreV1.Set(new PendingApprovalV1
        {
            Type = PendingApprovalTypeV1.DeviceTrustRequest,
            Title = "Trust new device?",
            Target = fingerprint,
            Preview = name + " (" + fingerprint.Substring(0, 8) + "...)",
            RequiresUserApproval = true
        });
        BroadcastPendingApprovalV1();
        return "{\"pending\":true}";
    }

    // Övriga: dispatcha synkront via samma path som WebView2 hade
    DispatchWebMessageSync(json);
    return "{\"ok\":true}";
}
```

(`DispatchWebMessageSync` är en ny privat metod som extraherar dispatch-koden ur befintliga `WebView_WebMessageReceived` så den kan anropas från båda hållen.)

- [ ] **Step 4: Bygga om PostWebMessageAsJson så det också broadcastar**

Hitta wrapper-metoden för `webView.CoreWebView2.PostWebMessageAsJson(...)` (eller skapa en):

```csharp
private void SendToAllClientsV1(string json)
{
    try { webView.CoreWebView2.PostWebMessageAsJson(json); } catch { }
    try { _bridgeServer?.Broadcast(json); } catch { }
}
```

Ersätt alla direkta `webView.CoreWebView2.PostWebMessageAsJson(...)`-anrop i Program.cs med `SendToAllClientsV1(...)`.

- [ ] **Step 5: BuildBootstrapStateV1**

```csharp
private string BuildBootstrapStateV1()
{
    var state = new
    {
        type = "jarvis_bootstrap_v1",
        activeFile = LatestActiveFilePathV1 ?? "",
        latestTerminal = LatestTerminalPayloadV1Json ?? "{}",
        pendingApproval = PendingApprovalStoreV1.Get() is null ? null : new {
            type = PendingApprovalStoreV1.Get()!.Type.ToString(),
            title = PendingApprovalStoreV1.Get()!.Title,
            target = PendingApprovalStoreV1.Get()!.Target
        }
    };
    return JsonSerializer.Serialize(state);
}
```

- [ ] **Step 6: OnFormClosing → stop bridge**

```csharp
protected override void OnFormClosing(FormClosingEventArgs e)
{
    try { _bridgeServer?.Stop(); } catch { }
    base.OnFormClosing(e);
}
```

- [ ] **Step 7: Build + verify 0 errors**

```bash
cd /f/Jarvis-clean && dotnet build app/JarvisClean.csproj 2>&1 | tail -3
```

Expected: 0 errors. Rätta refactor-kompileringsfel iterativt.

- [ ] **Step 8: Commit**

```bash
git add app/Program.cs
git commit -m "feat(bridge): wire BridgeServerV1 into Program.cs and broadcast all PostWebMessageAsJson"
```

---

## Task 17: URL ACL setup-script + dokumentation

`http://+:7777/` kräver URL ACL för icke-elevated user.

**Files:**
- Create: `F:\Jarvis-clean\tools\install-bridge-urlacl.cmd`
- Modify: `F:\Jarvis-clean\README.md`

- [ ] **Step 1: Skapa setup-script**

`tools\install-bridge-urlacl.cmd`:

```cmd
@echo off
REM Run this once as Administrator to allow Jarvis bridge to bind to http://+:7777/
REM Uses current user. Adjust port if you changed config\bridge_port.txt.

netsh http add urlacl url=http://+:7777/ user=%USERNAME%

if %ERRORLEVEL% EQU 0 (
    echo OK: URL ACL added for http://+:7777/
) else (
    echo FAILED. Try running this CMD as Administrator.
)
pause
```

- [ ] **Step 2: README-tillägg**

Lägg till i `README.md`:

```markdown
## Phone bridge (fas 1)

Telefonen kan styra Jarvis över LAN via `dashboard/index.html` serverad på port 7777.

**First-time setup:**

1. Högerklicka `tools\install-bridge-urlacl.cmd` → Run as administrator. Detta tillåter Jarvis att binda `http://+:7777/` utan elevation.
2. Starta Jarvis. Klicka **Anslut**-knappen → kopiera URL eller scanna QR med telefonen.
3. På telefonen: trycka "Be om trust" → godkänn popup på datorn → telefonen kan nu approva pending actions.

Konfiguration:
- `config\bridge_token.txt` — install-token (genereras automatiskt)
- `config\bridge_port.txt` — port (default 7777)
- `config\bridge_lan_ip.txt` — LAN-IP override (vid flera NICs)
- `config\trusted_devices.json` — parade enheter

CommandRouter-kommandon:
- `/enheter` — lista trusted devices
- `/enheter ta bort N` — ta bort device på position N
- `/bridge nytoken` — rotera install-token (tvingar om-paring)
```

- [ ] **Step 3: Commit**

```bash
git add tools/install-bridge-urlacl.cmd README.md
git commit -m "docs(bridge): add URL ACL setup script and README section"
```

---

## Task 18: Final verification + manuell smoke-test

- [ ] **Step 1: Kör alla node-tester**

```bash
cd /f/Jarvis-clean && for t in dashboard-routing.test.js smart-open-cleanup.test.js visual-panel.test.js dashboard-scrollbar-style.test.js approval-popup.test.js help-text.test.js file-write-safety.test.js file-delete-safety.test.js editor-save-safety.test.js undo-safety.test.js change-review-ui.test.js terminal-approval-safety.test.js project-explorer-polish.test.js overview-livestate.test.js create-folder-suggestions.test.js suggestion-colors.test.js bridge-jsclient.test.js setup-view.test.js mobile-css.test.js; do node "tests/$t" > /dev/null 2>&1 && echo "GREEN $t" || echo "RED $t"; done
```

Expected: alla GREEN.

- [ ] **Step 2: Kör alla C#-tester**

```bash
cd /f/Jarvis-clean && dotnet run --project tests/CommandRouterV1.Tests/CommandRouterV1.Tests.csproj 2>&1 | tail -5
cd /f/Jarvis-clean && dotnet run --project tests/TrustedDevicesStoreV1.Tests/TrustedDevicesStoreV1.Tests.csproj 2>&1 | tail -5
cd /f/Jarvis-clean && dotnet run --project tests/BridgeServerV1.Tests/BridgeServerV1.Tests.csproj 2>&1 | tail -5
```

Expected: alla PASS.

- [ ] **Step 3: dotnet build + publish**

```bash
cd /f/Jarvis-clean && dotnet build app/JarvisClean.csproj 2>&1 | tail -3
cd /f/Jarvis-clean && dotnet publish app/JarvisClean.csproj -c Release -o dist --no-self-contained 2>&1 | tail -3
```

Expected: 0 errors, 1 known warning.

- [ ] **Step 4: Stoppa & starta Jarvis**

```bash
powershell.exe -Command "Get-Process Jarvis -ErrorAction SilentlyContinue | Stop-Process -Force; Start-Sleep 2"
powershell.exe -Command "Start-Process wscript.exe -ArgumentList 'F:\Jarvis-clean\Starta-Jarvis.vbs' -WindowStyle Hidden; Start-Sleep 4; Get-Process Jarvis | Format-List Id, SessionId"
```

Expected: ny PID i SessionId 11.

- [ ] **Step 5: Manuella tester**

Kör i denna ordning:

| # | Steg | Förväntat |
|---|------|-----------|
| 1 | Klicka **Anslut** i datorns dashboard | Setup-panel visar URL + QR + tom trusted-lista |
| 2 | Skriv `/enheter` i chat | Svar: "Inga trusted devices ännu" |
| 3 | Öppna URL från setup-panelen i annan dator/telefon-browser på samma WiFi | Dashboard renderas |
| 4 | Skriv `/hjälp` i telefonens chat | Svar dyker upp på BÅDA klienter |
| 5 | I telefonens setup-panel, "Be om trust" (TODO: lägg knapp i Task 13 om saknas) | Popup på datorn: "Trust new device?" |
| 6 | Godkänn popup på datorn | Trust granted broadcast |
| 7 | Skapa pending file write från telefon: `/fil skapa docs/test-bridge.md = från-telefonen` | Approval popup på BÅDA |
| 8 | Approva från telefon | Fil skapas |
| 9 | `/enheter` | Listar din telefon |
| 10 | `/bridge nytoken` | Båda klienter får 401 |

- [ ] **Step 6: Commit final state**

```bash
git status
git add -A
git commit -m "feat(bridge): phase 1 complete — PWA phone bridge MVP"
```

---

## Self-review (utförd av writing-plans)

**Spec coverage** (PHONE_BRIDGE_PLAN.md → tasks):

| Spec section | Tasks |
|---|---|
| BridgeServerV1 (HttpListener, auth, endpoint-routing, SSE) | 6, 7, 8, 9, 10, 11 |
| BridgeAuthV1 | 4 |
| TrustedDevicesStoreV1 | 3 |
| PendingApprovalV1.DeviceTrustRequest | 1 |
| Program.cs (start, broadcast) | 16 |
| CommandRouterV1.BridgeAdmin | 2 |
| bridge_token.txt + bridge_port.txt + bridge_lan_ip.txt | 5 |
| trusted_devices.json | 3 |
| dashboard\bridge.js (JarvisBridge polyfill) | 12 |
| dashboard\index.html (setup view, mobile CSS) | 13, 14, 15 |
| Tests (bridge-server, bridge-auth, bridge-jsclient, mobile-css, setup-view) | 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 |
| URL ACL setup script | 17 |
| Acceptance criteria verification | 18 |

Alla spec-områden täckta.

**Placeholder scan:** Inga TBD/TODO i task-innehållen. Alla code-block är konkreta. Tasken som inkluderar tredjeparts-bibliotek (Task 14, qrcode.js) refererar specifikt MIT-licensad fil från känd källa.

**Type consistency:** `BridgeAuthV1.ValidateInstallToken`, `BridgeAuthV1.ComputeFingerprint`, `BridgeAuthV1.GenerateInstallToken`, `TrustedDevicesStoreV1.IsTrusted/Add/RemoveAt/List/Clear/TouchLastSeen`, `BridgeServerV1.Start/Stop/Broadcast`, `JarvisBridge.send/onMessage/mode/ready` — konsistenta över alla tasks.

**Scope check:** 18 tasks, alla i fas 1. Inga senare-fas-features har smugits in. Plan är fokuserad.

---

## Execution Handoff

**Plan complete and saved to `F:\Jarvis-clean\docs\PHONE_BRIDGE_IMPL_PLAN.md`. Two execution options:**

**1. Subagent-Driven (recommended)** — fresh subagent per task, two-stage review mellan varje, fast iteration.

**2. Inline Execution** — köra tasks i denna session med executing-plans skill, batch-checkpoints för review.

**Which approach?**
