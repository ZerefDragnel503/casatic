Set-Location -Path "$PSScriptRoot"
$py = Get-Content .\generate_socios_sql.py -Raw
$raw = ($py -split "raw_names = '''")[1] -split "'''"[0]
$lines = $raw -split "`r?`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" }
$extras = @('A&E Sistemas, S.A. de C.V.','Administración y Sistemas, S.A. de C.V.','Inversiones Digitales, S.A. de C.V.','AFP Crecer')
$names = $lines + $extras | ForEach-Object { $_.Trim() } | Select-Object -Unique
$header = "-- Socios data load generated from user list`nSET client_encoding = 'UTF8';`nSET standard_conforming_strings = on;`n`nTRUNCATE TABLE public.socios RESTART IDENTITY CASCADE;`n"
$outLines = New-Object System.Collections.Generic.List[string]
$outLines.Add($header)
foreach ($name in $names) {
    $uid = [guid]::NewGuid().ToString()
    $desc = 'Socio asociado del directorio CASATIC.'
    if ($name -match 'Universidad|Escuela') { $desc = 'Institución académica miembro del directorio CASATIC.' }
    elseif ($name -match 'Banco|AFP|Financiera|Financiero') { $desc = 'Entidad financiera asociada al directorio CASATIC.' }
    elseif ($name -match 'Consultores|Consultoría|Consultor|Consulting') { $desc = 'Empresa de consultoría tecnológica asociada al directorio CASATIC.' }
    elseif ($name -match 'Services|Solutions|Software|Tecnolog|Tecnica|Technology|Tecnologí') { $desc = 'Empresa tecnológica asociada al directorio CASATIC.' }
    $nameEsc = $name -replace "'","''"
    $descEsc = $desc -replace "'","''"
    $slug = $name.ToLower() -replace '[áéíóúüñÁÉÍÓÚÜÑ]','' -replace '\s+','-' -replace '[^a-z0-9\-]','' -replace '-+','-'
    $line = "INSERT INTO public.socios(\"Id\", \"NombreEmpresa\", \"Slug\", \"Descripcion\", \"Especialidades\", \"Servicios\", \"RsWebsite\", \"RsFacebook\", \"RsLinkedin\", \"RsTwitter\", \"RsInstagram\", \"RsYoutube\", \"Telefono\", \"Direccion\", \"LogoUrl\", \"EmailContacto\", \"MapaUrl\", \"MarcasRepresenta\", \"EstadoFinanciero\", \"Habilitado\") VALUES ('$uid', '$nameEsc', '$slug', '$descEsc', '{{}}', '{{}}', '', '', '', '', '', '', '', '', '', '', '', '', 'AlDia', true);"
    $outLines.Add($line)
}
$outLines | Set-Content -Path ".\backend\docker script\socios-current-backup-2026-05-29_115248.utf8.sql" -Encoding UTF8
Write-Host "Wrote $($names.Count) entries to backend\docker script\socios-current-backup-2026-05-29_115248.utf8.sql" 
