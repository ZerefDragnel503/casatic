# Backup script for socios table
# Usage: Run from repository root. Requires Docker and container 'casatic-db' running.

$ErrorActionPreference = 'Stop'

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$repoRoot = Split-Path -Parent $scriptDir
$destDir = Join-Path $repoRoot 'backend\docker script'

# Ensure destination directory exists
if (-not (Test-Path -Path $destDir)) {
  New-Item -ItemType Directory -Path $destDir | Out-Null
}

if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
  Write-Error "Docker no está instalado o no está en PATH."
  exit 1
}

# Ensure container is running
$container = 'casatic-db'
$inspect = docker ps --filter "name=$container" --format "{{.Names}}"
if (-not $inspect) {
  Write-Error "Contenedor $container no está en ejecución. Levántalo antes de ejecutar este script."
  exit 1
}

# Output files
$currentFile = Join-Path $destDir 'socios-current.sql'
$timestamp = Get-Date -Format 'yyyy-MM-dd_HHmmss'
$backupFile = Join-Path $destDir ("socios-current-backup-{0}.sql" -f $timestamp)

# Database connection details (from .env)
$pgUser = 'casatic'
$pgDb = 'casatic_directorio'

Write-Host "Exportando tabla 'socios' desde contenedor $container hacia: $currentFile"
# Use pg_dump to export only data as INSERTs
docker exec $container pg_dump -U $pgUser -d $pgDb -t socios -a --inserts > "$currentFile"
if ($LASTEXITCODE -ne 0) {
  Write-Error "pg_dump falló."
  exit 1
}

# Make timestamped backup copy
Copy-Item -Path $currentFile -Destination $backupFile -Force
Write-Host "Backup creado: $backupFile"
Write-Host "Actualizado: $currentFile"
