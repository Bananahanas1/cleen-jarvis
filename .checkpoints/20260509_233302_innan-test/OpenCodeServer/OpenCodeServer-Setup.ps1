# OpenCodeServer Setup Script (net8.0, local/local-host)
# This script builds, starts and validates the local OpenCodeServer.
# Prerequisites: .NET 8 SDK installed, PowerShell 5.1+ (or PowerShell 7+), port 6000 available.

Param()

$ErrorActionPreference = 'Stop'

$root = Split-Path -Path $MyInvocation.MyCommand.Path -Parent
$csproj = $null
$possiblePaths = @(
  (Join-Path $root 'OpenCodeServer.csproj'),
  (Join-Path $root 'OpenCodeServer/OpenCodeServer.csproj'),
  (Join-Path $root 'OpenCodeServer/OpenCodeServer/OpenCodeServer.csproj')
)
foreach ($p in $possiblePaths) {
  if (Test-Path $p) { $csproj = $p; break }
}
if (-not $csproj) {
  $found = Get-ChildItem -Path $root -Recurse -Filter '*.csproj' -File | Select-Object -First 1
  if ($found) { $csproj = $found.FullName }
}
if (-not $csproj) {
  Write-Error 'OpenCodeServer csproj not found under the project root'
  exit 1
}
$serverWorkingDir = $root

Write-Host "OpenCodeServer-Setup: Preparing environment..."

if (-not (Test-Path $csproj)) {
  Write-Error "OpenCodeServer csproj not found at $csproj"
  exit 1
}

# 1) Check .NET SDKs - require net8.0
$sdks = & dotnet --list-sdks
if (-not ($sdks -match "^\s*8\.")) {
  Write-Warning "NET8 SDK not found. Please install .NET 8 SDK from https://dotnet.microsoft.com/download/dotnet/8.0 and re-run."
  exit 1
}

Write-Host "NET8 SDK detected. Proceeding..."

# 2) Restore and build net8.0 output
& dotnet restore $csproj
& dotnet build $csproj -c Release -o .\OpenCodeServer\bin\Release\net8.0

Write-Host "Build completed. Starting server..."

# 3) Stop any running dotnet OpenCodeServer instances (best-effort)
Get-Process -Name dotnet -ErrorAction SilentlyContinue | ForEach-Object { try { Stop-Process -Id $_.Id -Force } catch { } }

 $quotedCsproj = '"' + $csproj + '"'
 Start-Process -FilePath dotnet -ArgumentList ("run -p " + $quotedCsproj) -WorkingDirectory $root -NoNewWindow

Write-Host "Server start initiated. Waiting for health..."

# 4) Health check loop
$healthUri = 'http://localhost:6000/health'
for ($i = 0; $i -lt 20; $i++) {
  try {
    $resp = Invoke-WebRequest -Uri $healthUri -UseBasicParsing -TimeoutSec 2
    if ($resp.StatusCode -eq 200) { Write-Host 'OpenCodeServer is up at http://localhost:6000'; break }
  } catch {
    # ignore and retry
  }
  Start-Sleep -Seconds 1
}

if ($LASTEXITCODE -ne 0) {
  Write-Warning 'Health check did not return 200 in time. Check OpenCodeServer logs.'
}
