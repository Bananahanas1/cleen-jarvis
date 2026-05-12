# PHONE_BRIDGE_IMPL_PLAN PART 02

Generated split from $path on 2026-05-12 to keep Markdown files under 14 000 characters. Original content continues below.

---


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
