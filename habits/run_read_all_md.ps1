<#
  Read all Markdown files and produce a readable summary for planning.
  This is a small automation to help enforce the habit of reading md-files before planning.
#>

$mdFiles = Get-ChildItem -Path . -Recurse -Include *.md | Where-Object { -not $_.PSIsContainer }

$summary = @()
foreach ($f in $mdFiles) {
  # Try to extract the first H1 or H2 as a title; fallback to first line
  $match = (Select-String -Path $f.FullName -Pattern '^#+\s*(.*)' -AllMatches)
  if ($match -and $match.Matches.Count -gt 0) {
    $title = $match.Matches[0].Groups[1].Value.Trim()
  } else {
    $title = (Get-Content -LiteralPath $f.FullName -TotalCount 1)[0]
  }
  $summary += [pscustomobject]@{ Path = $f.FullName; Title = $title; LastModified = $f.LastWriteTime }
}

$reportPath = Join-Path -Path (Get-Location) -ChildPath "MD_READ_SUMMARY.md"
"# MD Read Summary" | Out-File -FilePath $reportPath -Encoding UTF8
foreach ($row in $summary | Sort-Object Path) {
  $titleText = if ($row.Title) { $row.Title } else { "(no title)" }
  $line = "- [" + ([IO.Path]::GetFileName($row.Path)) + "](" + $row.Path + "): " + $titleText + " (LastModified: " + $row.LastModified.ToString("yyyy-MM-dd HH:mm") + ")"
  Add-Content -Path $reportPath -Value $line
}

Write-Output "MD read summary generated at $reportPath"
