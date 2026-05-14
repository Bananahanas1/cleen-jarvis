param(
    [int]$BackendPort = 8787,
    [int]$FrontendPort = 5177
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$VenvPython = Join-Path $Root ".venv\Scripts\python.exe"
$LogDir = Join-Path $Root "logs"

if (-not (Test-Path $VenvPython)) {
    throw "Amy is not installed. Run .\Install-AmyWindows.ps1 first."
}

New-Item -ItemType Directory -Force -Path $LogDir | Out-Null
if (-not (Test-Path (Join-Path $Root ".env"))) {
    Copy-Item -LiteralPath (Join-Path $Root ".env.example") -Destination (Join-Path $Root ".env")
}

$backendCommand = "cd /d `"$Root`"; `"$VenvPython`" -m uvicorn backend.app.main:app --host 127.0.0.1 --port $BackendPort"
$frontendCommand = "cd /d `"$Root\frontend`"; npm run dev -- --host 127.0.0.1 --port $FrontendPort"

Start-Process powershell -ArgumentList "-NoExit", "-Command", $backendCommand -WorkingDirectory $Root
Start-Sleep -Seconds 2
Start-Process powershell -ArgumentList "-NoExit", "-Command", $frontendCommand -WorkingDirectory (Join-Path $Root "frontend")

Start-Process "http://127.0.0.1:$FrontendPort"
Write-Host "Amy Windows startar."
Write-Host "Frontend: http://127.0.0.1:$FrontendPort"
Write-Host "Backend:  http://127.0.0.1:$BackendPort"
