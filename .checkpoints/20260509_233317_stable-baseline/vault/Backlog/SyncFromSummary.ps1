<#
SyncFromSummary.ps1
Reads MD_READ_SUMMARY.md and appends simple backlog items to FasA-MVP-Tasks.md.
This is a lightweight helper to keep the MVP backlog in sync with current docs.
#>

$summaryPath = Join-Path -Path (Get-Location) -ChildPath "MD_READ_SUMMARY.md"
$targetBacklog = Join-Path -Path (Get-Location) -ChildPath "vault/Backlog/FasA-MVP-Tasks.md"

if (-Not (Test-Path -Path $summaryPath)) {
  Write-Error "Summary file not found: $summaryPath"
  exit 1
}

if (-Not (Test-Path -Path $targetBacklog)) {
  New-Item -ItemType File -Path $targetBacklog -Force | Out-Null
}

foreach ($line in Get-Content $summaryPath) {
  if ($line -match '^- \[(.*?)\]\((.*?)\): (.*)$') {
    $sourceName = $Matches[1]
    $sourcePath = $Matches[2]
    $desc = $Matches[3]
    $entry = "- [ ] Review: $sourceName (source: $sourcePath) : $desc"
    Add-Content -Path $targetBacklog -Value $entry
  }
}

Write-Host "Synced summary entries into FasA-MVP-Tasks.md"
