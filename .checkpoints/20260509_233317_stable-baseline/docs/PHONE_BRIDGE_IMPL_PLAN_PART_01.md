# PHONE_BRIDGE_IMPL_PLAN PART 01

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---

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
