# PHONE_BRIDGE_IMPL_PLAN PART 04

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---


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
