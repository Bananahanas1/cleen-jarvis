param(
    [switch]$SkipNode,
    [switch]$SkipDotnet,
    [switch]$SkipBuild,
    [switch]$SkipMarkdown
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$runId = Get-Date -Format "yyyyMMdd-HHmmss"
$logDir = Join-Path $repoRoot "data/test-runs/$runId"
$summaryPath = Join-Path $logDir "summary.txt"
$failures = New-Object System.Collections.Generic.List[string]

function Write-TestLog {
    param([string]$Message)
    $line = "$(Get-Date -Format 'HH:mm:ss')  $Message"
    Write-Host $line
    for ($attempt = 1; $attempt -le 8; $attempt++) {
        try {
            Add-Content -LiteralPath $summaryPath -Value $line
            return
        }
        catch {
            if ($attempt -eq 8) {
                throw
            }
            Start-Sleep -Milliseconds 150
        }
    }
}

function Run-Step {
    param(
        [string]$Name,
        [scriptblock]$Action
    )

    Write-TestLog ""
    Write-TestLog "== $Name =="
    try {
        & $Action
        Write-TestLog "PASS $Name"
    }
    catch {
        $failures.Add($Name)
        Write-TestLog "FAIL $Name"
        Write-TestLog $_.Exception.Message
    }
}

function Run-NodeRegression {
    $testFiles = Get-ChildItem -LiteralPath (Join-Path $repoRoot "tests") -Filter "*.test.js" | Sort-Object Name
    $testFiles | ForEach-Object {
        Write-TestLog "node $($_.Name)"
        node $_.FullName
        if ($LASTEXITCODE -ne 0) {
            throw "Node-test failed: $($_.Name)"
        }
    }

    Write-TestLog "Node regression passed: $($testFiles.Count) test files"
}

function Run-CommandRouterTests {
    $project = Join-Path $repoRoot "tests/CommandRouterV1.Tests/CommandRouterV1.Tests.csproj"
    dotnet run --project $project
    if ($LASTEXITCODE -ne 0) {
        throw "CommandRouterV1.Tests failed"
    }
}

function Run-AppBuild {
    $project = Join-Path $repoRoot "app/JarvisClean.csproj"
    dotnet build $project
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet build failed"
    }
}

function Test-MarkdownLength {
    $tooLong = @()
    Get-ChildItem -LiteralPath $repoRoot -Recurse -Filter "*.md" |
        Where-Object { $_.FullName -notmatch "\\(bin|obj|dist|\.git|node_modules|\.checkpoints|archive|superpowers)\\" } |
        ForEach-Object {
            $chars = (Get-Content -LiteralPath $_.FullName -Raw).Length
            if ($chars -gt 14000) {
                $tooLong += [pscustomobject]@{
                    FullName = $_.FullName
                    Chars = $chars
                }
            }
        }

    if ($tooLong.Count -gt 0) {
        $tooLong | Format-Table -AutoSize | Out-String | ForEach-Object { Write-TestLog $_ }
        throw "Markdown files over 14000 chars: $($tooLong.Count)"
    }

    Write-TestLog "Markdown length passed: all .md files under 14000 chars"
}

New-Item -ItemType Directory -Force -Path $logDir | Out-Null
Set-Content -LiteralPath $summaryPath -Value "Jarvis full smoke test $runId"

Write-TestLog "Repo: $repoRoot"
Write-TestLog "Logs: $logDir"

if (-not $SkipNode) {
    Run-Step "Node regression suite" { Run-NodeRegression }
}

if (-not $SkipDotnet) {
    Run-Step "CommandRouterV1 C# tests" { Run-CommandRouterTests }
}

if (-not $SkipBuild) {
    Run-Step "Jarvis app build" { Run-AppBuild }
}

if (-not $SkipMarkdown) {
    Run-Step "Markdown length guard" { Test-MarkdownLength }
}

Write-TestLog ""
if ($failures.Count -gt 0) {
    Write-TestLog "FAILED CHECKS: $($failures -join ', ')"
    Write-TestLog "Summary: $summaryPath"
    exit 1
}

Write-TestLog "ALL CHECKS PASSED"
Write-TestLog "Summary: $summaryPath"
