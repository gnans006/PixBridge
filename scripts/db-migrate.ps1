#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Applies pending EF Core database migrations to the PixBridge database.

.USAGE
    .\scripts\db-migrate.ps1
    .\scripts\db-migrate.ps1 -SkipStopServices    # skip stopping running processes
#>

param(
    [switch]$SkipStopServices
)

$ErrorActionPreference = 'Stop'
$Root  = Split-Path $PSScriptRoot -Parent
$Infra = Join-Path $Root "src\EventPhoto.Infrastructure"
$Api   = Join-Path $Root "src\EventPhoto.Api"

function Write-Step([string]$msg) {
    Write-Host "`n━━━  $msg  ━━━" -ForegroundColor Cyan
}

Write-Host "`n━━━  PixBridge DB Migration  ━━━" -ForegroundColor Magenta

# ── Stop running processes (they lock DLLs during build) ─────────────────────
if (-not $SkipStopServices) {
    Write-Step "Stopping running PixBridge processes"
    foreach ($name in @('EventPhoto.Api', 'EventPhoto.Worker')) {
        $procs = Get-Process -Name $name -ErrorAction SilentlyContinue
        if ($procs) {
            Write-Host "  Stopping $name (PID $($procs.Id -join ', '))..." -ForegroundColor Yellow
            $procs | Stop-Process -Force
            Start-Sleep -Milliseconds 600
        }
    }
}

# ── Check dotnet-ef tool ──────────────────────────────────────────────────────
Write-Step "Checking dotnet-ef tool"
$efInstalled = dotnet tool list --global 2>$null | Select-String "dotnet-ef"
if (-not $efInstalled) {
    Write-Host "  Installing dotnet-ef..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
}
Write-Host "  dotnet-ef ready." -ForegroundColor Green

# ── Show pending migrations ───────────────────────────────────────────────────
Write-Step "Current migration status"
Push-Location $Root
dotnet ef migrations list `
    --project $Infra `
    --startup-project $Api
Pop-Location

# ── Apply migrations ──────────────────────────────────────────────────────────
Write-Step "Applying pending migrations"
Push-Location $Root
dotnet ef database update `
    --project $Infra `
    --startup-project $Api

if ($LASTEXITCODE -ne 0) {
    Write-Host "`n  Migration failed. Common causes:" -ForegroundColor Red
    Write-Host "    • PostgreSQL is not running on localhost:5432"
    Write-Host "    • Database 'pixbridge_dev' does not exist"
    Write-Host "    • Wrong password in AppDbContextFactory.cs or appsettings.json"
    Pop-Location
    exit 1
}
Pop-Location

Write-Host "`n✔  Migrations applied successfully." -ForegroundColor Green
