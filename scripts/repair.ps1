#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Repairs a failed or partial PixBridge installation.
    Run this after a failed install or after fixing a prerequisite (e.g. wrong DB password, port conflict).

.PARAMETER Step
    Which step to repair:
      All       — Re-run every step (default)
      DB        — Re-create PostgreSQL database/user only
      Services  — Re-install Windows services only
      Network   — Re-apply firewall and Wi-Fi profile only

.PARAMETER InstallDir
    The PixBridge installation folder. Default: auto-detect from script location.

.PARAMETER PgPassword
    The postgres superuser password (only needed if setup-postgresql.ps1 failed due to auth).

.EXAMPLE
    .\repair.ps1
    .\repair.ps1 -Step DB -PgPassword "yourPostgresPassword"
    .\repair.ps1 -Step Services
#>

param(
    [ValidateSet("All", "DB", "Services", "Network")]
    [string]$Step       = "All",
    [string]$InstallDir = "",
    [string]$PgPassword = ""
)

$ErrorActionPreference = "Stop"

# ── Resolve install directory ─────────────────────────────────────────────
if (-not $InstallDir) {
    # Primary: when run from {app}\scripts\repair.ps1, go one level up
    $candidate = Split-Path $PSScriptRoot -Parent
    if ($candidate -and (Test-Path (Join-Path $candidate "scripts\setup-postgresql.ps1"))) {
        $InstallDir = $candidate
    }
}

if (-not $InstallDir -or -not (Test-Path (Join-Path $InstallDir "scripts\setup-postgresql.ps1"))) {
    # Fallback: search common install roots (default autopf + custom locations)
    $searchRoots = @(
        "$env:ProgramFiles\PixBridge",
        "${env:ProgramFiles(x86)}\PixBridge",
        "C:\PixBridge",
        "D:\PixBridge"
    )
    foreach ($root in $searchRoots) {
        if (Test-Path (Join-Path $root "scripts\setup-postgresql.ps1")) {
            $InstallDir = $root
            break
        }
    }
}

if (-not $InstallDir -or -not (Test-Path $InstallDir)) {
    Write-Host "Cannot find PixBridge install directory." -ForegroundColor Red
    Write-Host "Pass -InstallDir explicitly, e.g.:" -ForegroundColor Yellow
    Write-Host "  .\repair.ps1 -InstallDir 'C:\Program Files\PixBridge'" -ForegroundColor Yellow
    exit 1
}

$Scripts = Join-Path $InstallDir "scripts"
$LogDir  = Join-Path $InstallDir "logs"
New-Item -ItemType Directory -Force -Path $LogDir | Out-Null

function Section { param([string]$Title) Write-Host "`n━━━  $Title  ━━━" -ForegroundColor Cyan }
function OK      { param([string]$Msg)   Write-Host "  ✔  $Msg" -ForegroundColor Green }
function FAIL    { param([string]$Msg)   Write-Host "  ✖  $Msg" -ForegroundColor Red }

# ── Step: DB ──────────────────────────────────────────────────────────────
function Repair-DB {
    Section "Repairing Database Setup"
    $script = Join-Path $Scripts "setup-postgresql.ps1"
    $log    = Join-Path $LogDir  "setup-db.log"

    if (-not (Test-Path $script)) {
        FAIL "setup-postgresql.ps1 not found at $script"
        exit 1
    }

    $params = @("-LogFile", $log)
    if ($PgPassword) { $params += @("-PgPassword", $PgPassword) }

    Write-Host "  Running setup-postgresql.ps1..." -ForegroundColor Yellow
    & powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $script @params
    if ($LASTEXITCODE -ne 0) {
        FAIL "Database setup failed. See log: $log"
        Get-Content $log | Select-Object -Last 10 | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
        exit 1
    }
    OK "Database setup completed. Log: $log"
}

# ── Step: Services ────────────────────────────────────────────────────────
function Repair-Services {
    Section "Repairing Windows Service Installation"
    $script = Join-Path $Scripts "install-service.ps1"
    $log    = Join-Path $LogDir  "setup-services.log"

    if (-not (Test-Path $script)) {
        FAIL "install-service.ps1 not found at $script"
        exit 1
    }

    # Check API exe exists
    $apiExe = Join-Path $InstallDir "api\EventPhoto.Api.exe"
    if (-not (Test-Path $apiExe)) {
        FAIL "API executable not found: $apiExe"
        Write-Host "  The install folder may be incomplete. Re-run the full installer." -ForegroundColor Yellow
        exit 1
    }

    # Check port 5000
    $portInUse = Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue
    if ($portInUse) {
        $proc = Get-Process -Id $portInUse.OwningProcess -ErrorAction SilentlyContinue
        FAIL "Port 5000 is in use by '$($proc.Name)'. Stop it first then rerun repair."
        exit 1
    }

    Write-Host "  Running install-service.ps1..." -ForegroundColor Yellow
    & powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $script -InstallDir $InstallDir -LogFile $log
    if ($LASTEXITCODE -ne 0) {
        FAIL "Service installation failed. See log: $log"
        Get-Content $log | Select-Object -Last 10 | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
        exit 1
    }
    OK "Services installed and running. Log: $log"
}

# ── Step: Network ─────────────────────────────────────────────────────────
function Repair-Network {
    Section "Repairing Firewall and Network Settings"
    $script = Join-Path $Scripts "fix-network-access.ps1"
    if (-not (Test-Path $script)) {
        FAIL "fix-network-access.ps1 not found at $script"
        exit 1
    }
    & powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File $script
    if ($LASTEXITCODE -ne 0) {
        FAIL "Network fix failed."
        exit 1
    }
    OK "Firewall and Wi-Fi profile configured."
}

# ── Service health check ──────────────────────────────────────────────────
function Show-ServiceStatus {
    Section "Service Status"
    foreach ($svc in @("PixBridgeApi", "PixBridgeWorker")) {
        $s = Get-Service -Name $svc -ErrorAction SilentlyContinue
        if (-not $s) {
            Write-Host "  $svc — NOT INSTALLED" -ForegroundColor Red
        } elseif ($s.Status -eq "Running") {
            OK "$svc — Running"
        } else {
            Write-Host "  $svc — $($s.Status)" -ForegroundColor Yellow
        }
    }

    # Quick HTTP health check
    try {
        $resp = Invoke-WebRequest -Uri "http://localhost:5000/api/health" -UseBasicParsing -TimeoutSec 5
        OK "API health check: HTTP $($resp.StatusCode)"
    } catch {
        Write-Host "  API health check: FAILED — $($_.Exception.Message)" -ForegroundColor Red
        Write-Host "  This is normal if services just started. Wait 10 s then retry." -ForegroundColor Yellow
    }
}

# ── Dispatch ──────────────────────────────────────────────────────────────
Write-Host "`n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Cyan
Write-Host " PixBridge Repair Tool — Step: $Step" -ForegroundColor Cyan
Write-Host " Install dir: $InstallDir" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━`n" -ForegroundColor Cyan

switch ($Step) {
    "DB"       { Repair-DB }
    "Services" { Repair-Services }
    "Network"  { Repair-Network }
    "All" {
        Repair-DB
        Repair-Network
        Repair-Services
    }
}

Show-ServiceStatus

Write-Host "`nRepair complete. Open http://localhost:5000/admin to verify." -ForegroundColor Green
