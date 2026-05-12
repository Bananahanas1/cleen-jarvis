# PHONE_BRIDGE_IMPL_PLAN PART 03

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---


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
