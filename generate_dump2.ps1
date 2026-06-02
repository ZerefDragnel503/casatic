Set-Location -Path "$PSScriptRoot"
$py = Get-Content .\generate_socios_sql.py -Raw
$pattern = "raw_names = '''(.*?)'''"
$match = [regex]::Match($py, $pattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
if (-not $match.Success) { Write-Error 'No raw_names block found'; exit 1 }
$raw = $match.Groups[1].Value
$lines = $raw -split "`r`n|`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }
$seen = New-Object System.Collections.Generic.HashSet[string]
$names = New-Object System.Collections.Generic.List[string]
foreach ($l in $lines) {
    $norm = $l
    # Apply aliases mapping from Python file if present
    $aliasesPattern = "aliases\s*=\s*\{(.*?)\n\}\n" 
    $aliasesMatch = [regex]::Match($py, $aliasesPattern, [System.Text.RegularExpressions.RegexOptions]::Singleline)
    if ($aliasesMatch.Success) {
        $aliasesBlock = $aliasesMatch.Groups[1].Value
        $aliasMap = @{}
        $aliasRegex = "'([^']+)':\s*'([^']+)'"
        foreach ($m in [regex]::Matches($aliasesBlock, $aliasRegex)) { $aliasMap[$m.Groups[1].Value] = $m.Groups[2].Value }
        if ($aliasMap.ContainsKey($norm)) { $norm = $aliasMap[$norm] }
    }
    $norm = $norm -replace '\s{2,}',' ' -replace '\s+$',''
    # Remove trailing dot unless in exceptions
    $exceptions = @('RSM','ALFI','HAKKI')
    if ($norm.EndsWith('.') -and ($exceptions -notcontains $norm)) { $norm = $norm.Substring(0,$norm.Length-1) }
    if (-not $seen.Contains($norm)) { [void]$seen.Add($norm); $names.Add($norm) }
}
# extras
$extras = @('A&E Sistemas, S.A. de C.V.','Administración y Sistemas, S.A. de C.V.','Inversiones Digitales, S.A. de C.V.','AFP Crecer')
foreach ($e in $extras) { if (-not $seen.Contains($e)) { [void]$seen.Add($e); $names.Add($e) } }

# Build SQL
$header = @'
-- Socios data load generated from user list
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;

TRUNCATE TABLE public.socios RESTART IDENTITY CASCADE;
'@
$out = New-Object System.Text.StringBuilder
[void]$out.AppendLine($header)

$idx = 1
foreach ($name in $names) {
    $uid = ('{0:D8}-{1:D4}-{2:D4}-{3:D4}-{4:D12}' -f 0,0,0,0,$idx)
    $desc = 'Socio asociado del directorio CASATIC.'
    if ($name -match 'Universidad|Escuela') { $desc = 'Institución académica miembro del directorio CASATIC.' }
    elseif ($name -match 'Banco|AFP|Financiera|Financiero') { $desc = 'Entidad financiera asociada al directorio CASATIC.' }
    elseif ($name -match 'Consultores|Consultoría|Consultor|Consulting') { $desc = 'Empresa de consultoría tecnológica asociada al directorio CASATIC.' }
    elseif ($name -match 'Services|Solutions|Software|Tecnolog|Tecnica|Technology') { $desc = 'Empresa tecnológica asociada al directorio CASATIC.' }
    $nameEsc = $name.Replace("'","''")
    $descEsc = $desc.Replace("'","''")
    $slug = 's' + $idx.ToString('D4')
    $vals = @("'" + $uid + "'", "'" + $nameEsc + "'", "'" + $slug + "'", "'" + $descEsc + "'", "'{}'", "'{}'", "''", "''", "''", "''", "''", "''", "''", "''", "''", "''", "''", "''", "'AlDia'", "true")
    $line = 'INSERT INTO public.socios("Id", "NombreEmpresa", "Slug", "Descripcion", "Especialidades", "Servicios", "RsWebsite", "RsFacebook", "RsLinkedin", "RsTwitter", "RsInstagram", "RsYoutube", "Telefono", "Direccion", "LogoUrl", "EmailContacto", "MapaUrl", "MarcasRepresenta", "EstadoFinanciero", "Habilitado") VALUES (' + ($vals -join ', ') + ');'
    [void]$out.AppendLine($line)
    $idx++
}
$out.ToString() | Set-Content -Path ".\backend\docker script\02-socios-current.sql" -Encoding UTF8
Write-Host "Wrote $($names.Count) entries to backend\docker script\02-socios-current.sql"
