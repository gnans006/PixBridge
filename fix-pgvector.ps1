#Requires -RunAsAdministrator
# One-shot fix: copies bundled pgvector 0.8.0 files to PostgreSQL 17
# and verifies the extension is visible.
#
# Run from an ELEVATED PowerShell terminal at the repo root:
#   Set-ExecutionPolicy Bypass -Scope Process -Force
#   .\fix-pgvector.ps1

$repo    = $PSScriptRoot
$bundle  = Join-Path $repo "tools\pgvector\pg17"
$pgShare = "C:\Program Files\PostgreSQL\17\share\extension"
$pgLib   = "C:\Program Files\PostgreSQL\17\lib"

if (!(Test-Path $bundle)) {
    Write-Host "ERROR: Bundle not found at $bundle" -ForegroundColor Red
    Write-Host "  Run: git pull   then try again." -ForegroundColor Yellow
    exit 1
}

Write-Host "Copying pgvector files to PostgreSQL 17..." -ForegroundColor Cyan
Copy-Item "$bundle\vector.dll"     $pgLib   -Force
Copy-Item "$bundle\vector.control" $pgShare -Force
Get-ChildItem $bundle -Filter "*.sql" | ForEach-Object {
    Copy-Item $_.FullName $pgShare -Force
}
$count = (Get-ChildItem $bundle -Filter "*.sql").Count
Write-Host "  Copied $count SQL files + vector.dll + vector.control" -ForegroundColor Green

Write-Host "Restarting PostgreSQL..." -ForegroundColor Cyan
$svc = Get-Service "postgresql*" -ErrorAction SilentlyContinue | Select-Object -First 1
if ($svc) {
    Restart-Service $svc.Name -Force
    Start-Sleep -Seconds 5
    Write-Host "  Service '$($svc.Name)' restarted." -ForegroundColor Green
} else {
    Write-Host "  PostgreSQL service not found — restart it manually." -ForegroundColor Yellow
}

Write-Host "Verifying..." -ForegroundColor Cyan
$result = & "C:\Program Files\PostgreSQL\17\bin\psql.exe" -U postgres -d postgres `
    -c "SELECT name, default_version FROM pg_available_extensions WHERE name='vector';" 2>&1
if ($result -match "vector") {
    Write-Host "  pgvector is visible to PostgreSQL!" -ForegroundColor Green
    Write-Host $result -ForegroundColor Gray
    Write-Host "`nNow run: .\scripts\setup-face-search.ps1 -ServiceOnly" -ForegroundColor Cyan
} else {
    Write-Host "  STILL not visible. Files in $pgShare`:" -ForegroundColor Red
    Get-ChildItem $pgShare -Filter "vector*" | Select-Object Name
}
