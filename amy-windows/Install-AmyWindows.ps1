param(
    [switch]$InstallBrowser
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Venv = Join-Path $Root ".venv"
$VenvPython = Join-Path $Venv "Scripts\python.exe"

function Invoke-SystemPython {
    param([string[]]$PythonArgs)
    $py = Get-Command py -ErrorAction SilentlyContinue
    if ($py) {
        foreach ($version in @("-3.13", "-3.12", "-3.11", "-3")) {
            & $py.Source $version -c "import sys; print(sys.version)" *> $null
            if ($LASTEXITCODE -eq 0) {
                & $py.Source $version @PythonArgs
                return
            }
        }
    }

    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($python) {
        & $python.Source @PythonArgs
        return
    }

    throw "Python launcher not found. Install Python 3 first."
}

Set-Location $Root
if (-not (Test-Path $VenvPython)) {
    Invoke-SystemPython -PythonArgs @("-m", "venv", $Venv)
}

& $VenvPython -m pip install --upgrade pip
& $VenvPython -m pip install -r (Join-Path $Root "requirements.txt")

Push-Location (Join-Path $Root "frontend")
try {
    npm install
}
finally {
    Pop-Location
}

if ($InstallBrowser) {
    & $VenvPython -m playwright install chromium
}

if (-not (Test-Path (Join-Path $Root ".env"))) {
    Copy-Item -LiteralPath (Join-Path $Root ".env.example") -Destination (Join-Path $Root ".env")
}

Write-Host "Amy Windows install klar."
Write-Host "Starta med: .\Start-AmyWindows.ps1"
