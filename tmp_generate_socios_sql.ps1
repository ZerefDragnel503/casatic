$sourcePath = 'generate_socios_sql.py'
$sourceLines = Get-Content $sourcePath

# Extract raw_names block
$start = -1
for ($i = 0; $i -lt $sourceLines.Count; $i++) {
    if ($sourceLines[$i] -like "*raw_names = '''*") { $start = $i; break }
}
if ($start -lt 0) { throw 'raw_names block start not found' }
$rawNames = @()
$firstLine = $sourceLines[$start].Substring($sourceLines[$start].IndexOf("'''") + 3)
if ($firstLine.Trim()) { $rawNames += $firstLine.Trim() }
for ($i = $start + 1; $i -lt $sourceLines.Count; $i++) {
    if ($sourceLines[$i] -match "'''") {
        $linePart = $sourceLines[$i].Substring(0, $sourceLines[$i].IndexOf("'''")).Trim()
        if ($linePart) { $rawNames += $linePart }
        break
    }
    $rawNames += $sourceLines[$i].Trim()
}
$rawNames = $rawNames | Where-Object { $_ -ne '' }

# Extract aliases
$aliases = @{}
$aliasStart = ($sourceLines | Select-Object -Index ($sourceLines.FindIndex({ $_ -match '^aliases\s*=\s*{' })))
$aliasStartIndex = -1
for ($i = 0; $i -lt $sourceLines.Count; $i++) {
    if ($sourceLines[$i] -match '^aliases\s*=\s*{') { $aliasStartIndex = $i; break }
}
if ($aliasStartIndex -ge 0) {
    for ($i = $aliasStartIndex + 1; $i -lt $sourceLines.Count; $i++) {
        if ($sourceLines[$i] -match '^}') { break }
        if ($sourceLines[$i] -match "^\s*'(.+?)'\s*:\s*'(.+?)',?\s*$") {
            $aliases[$matches[1]] = $matches[2]
        }
    }
}

# Extract extra list
$extra = @()
$extraStartIndex = -1
for ($i = 0; $i -lt $sourceLines.Count; $i++) {
    if ($sourceLines[$i] -match '^extra\s*=\s*\[') { $extraStartIndex = $i; break }
}
if ($extraStartIndex -ge 0) {
    for ($i = $extraStartIndex + 1; $i -lt $sourceLines.Count; $i++) {
        if ($sourceLines[$i] -match '^\]') { break }
        if ($sourceLines[$i] -match "^\s*'(.+?)',?\s*$") {
            $extra += $matches[1]
        }
    }
}

# Normalize and dedupe
$seen = @{}
$names = @()
foreach ($line in $rawNames) {
    $normalized = $aliases.ContainsKey($line) ? $aliases[$line] : $line
    $normalized = $normalized -replace ' {2,}', ' '
    $normalized = $normalized.Trim()
    if ($normalized.EndsWith('.') -and $normalized -notin @('RSM','ALFI','HAKKI')) {
        $normalized = $normalized.TrimEnd('.')
    }
    if (-not $seen.ContainsKey($normalized)) {
        $seen[$normalized] = $true
        $names += $normalized
    }
}
foreach ($item in $extra) {
    if (-not $seen.ContainsKey($item)) {
        $seen[$item] = $true
        $names += $item
    }
}

# Slug helper
$accentMap = @{
    'Á'='A'; 'É'='E'; 'Í'='I'; 'Ó'='O'; 'Ú'='U'; 'Ü'='U'; 'Ñ'='N';
    'á'='a'; 'é'='e'; 'í'='i'; 'ó'='o'; 'ú'='u'; 'ü'='u'; 'ñ'='n'
}
function Remove-Accents($text) {
    foreach ($k in $accentMap.Keys) { $text = $text -replace [regex]::Escape($k), $accentMap[$k] }
    return $text
}
function Slugify($text) {
    $s = Remove-Accents($text)
    $s = $s.ToLowerInvariant()
    $s = $s -replace '\s+', '-'
    $s = $s -replace '[^a-z0-9\-]', '-'
    $s = $s -replace '-{2,}', '-'
    return $s.Trim('-')
}
function SqlEscape($value) {
    return $value -replace "'", "''"
}

$header = @"-- Socios data load generated from user list
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;

TRUNCATE TABLE public.socios RESTART IDENTITY CASCADE;
"@

$rows = @($header)
foreach ($name in $names) {
    $slug = Slugify($name)
    if (-not $slug) { $slug = [guid]::NewGuid().ToString() }
    $desc = 'Socio asociado del directorio CASATIC.'
    if ($name -match 'Universidad|Escuela') { $desc = 'Institución académica miembro del directorio CASATIC.' }
    elseif ($name -match 'Banco|AFP|Financiera|Financiero') { $desc = 'Entidad financiera asociada al directorio CASATIC.' }
    elseif ($name -match 'Consultores|Consultoría|Consultor|Consulting') { $desc = 'Empresa de consultoría tecnológica asociada al directorio CASATIC.' }
    elseif ($name -match 'Services|Solutions|Software|Tecnologías|Tecnologica|Technology|Tecnologí') { $desc = 'Empresa tecnológica asociada al directorio CASATIC.' }
    elseif ($name -match 'Académica|Academia|Institución|Instituto') { $desc = 'Institución académica miembro del directorio CASATIC.' }
    $desc = SqlEscape($desc)
    $escapedName = SqlEscape($name)
    $id = [guid]::NewGuid().ToString()
    $rows += "INSERT INTO public.socios(\"Id\", \"NombreEmpresa\", \"Slug\", \"Descripcion\", \"Especialidades\", \"Servicios\", \"RsWebsite\", \"RsFacebook\", \"RsLinkedin\", \"RsTwitter\", \"RsInstagram\", \"RsYoutube\", \"Telefono\", \"Direccion\", \"LogoUrl\", \"EmailContacto\", \"MapaUrl\", \"MarcasRepresenta\", \"EstadoFinanciero\", \"Habilitado\") VALUES ('$id', '$escapedName', '$slug', '$desc', '{ }', '{ }', '', '', '', '', '', '', '', '', '', '', '', '', 'AlDia', true);"
}
$content = $rows -join "`n" + "`n"
$outPath = 'backend\docker script\02-socios-current.sql'
Set-Content -Path $outPath -Value $content -Encoding UTF8
Write-Output "Generated $($names.Count) unique entries into $outPath"
