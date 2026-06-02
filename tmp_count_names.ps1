$lines = Get-Content 'generate_socios_sql.py'
$start = $null
$end = $null
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -like "*raw_names = '''*") { $start = $i; break }
}
if ($start -eq $null) { Write-Error 'start not found'; exit 1 }
$raw = @()
$firstLine = $lines[$start].Substring($lines[$start].IndexOf("'''") + 3)
if ($firstLine.Trim() -ne '') { $raw += $firstLine.Trim() }
for ($i = $start + 1; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match "'''") {
        $rawLine = $lines[$i].Substring(0, $lines[$i].IndexOf("'''"))
        if ($rawLine.Trim() -ne '') { $raw += $rawLine.Trim() }
        $end = $i
        break
    }
    $raw += $lines[$i].Trim()
}
if ($end -eq $null) { Write-Error 'end not found'; exit 1 }
$raw = $raw | Where-Object { $_ -ne '' }
$unique = $raw | Sort-Object -Unique
Write-Output "total=$($raw.Count)"
Write-Output "unique=$($unique.Count)"
$dupes = $raw | Group-Object | Where-Object { $_.Count -gt 1 }
if ($dupes.Count -gt 0) {
    Write-Output 'duplicates:'
    $dupes | ForEach-Object { Write-Output "  $($_.Name) => $($_.Count)" }
}
