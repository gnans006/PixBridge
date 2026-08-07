#!/usr/bin/env pwsh
<#
.SYNOPSIS
    PixBridge — one-shot setup script for a fresh machine or after pulling latest code.
    Runs: dotnet restore → EF migrations → npm install

.USAGE
    .\setup.ps1
    .\setup.ps1 -SkipMigration      # skip DB migration (no PostgreSQL available yet)
    .\setup.ps1 -SkipNpm            # skip npm install
    .\setup.ps1 -Build              # also compile the .NET solution and React app after setup
#>

param(
    [switch]$SkipMigration,
    [switch]$SkipNpm,
    [switch]$Build
)

$ErrorActionPreference = 'Stop'
$Root  = $PSScriptRoot
$Api   = Join-Path $Root "src\EventPhoto.Api"
$Infra = Join-Path $Root "src\EventPhoto.Infrastructure"
$React = Join-Path $Root "src\EventPhoto.React"

# ── helpers ──────────────────────────────────────────────────────────────────
function Write-Step([string]$msg) {
    Write-Host "`n━━━  $msg  ━━━" -ForegroundColor Cyan
}

function Assert-Command([string]$cmd) {
    if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
        Write-Host "ERROR: '$cmd' not found in PATH. Please install it and re-run." -ForegroundColor Red
        exit 1
    }
}

# ── pre-flight checks ─────────────────────────────────────────────────────────
Write-Step "Pre-flight checks"
Assert-Command "dotnet"
Assert-Command "npm"

$dotnetVersion = dotnet --version
$nodeVersion   = node --version
$npmVersion    = npm --version
Write-Host "  dotnet : $dotnetVersion"
Write-Host "  node   : $nodeVersion"
Write-Host "  npm    : $npmVersion"

# ── Step 1 · NuGet restore ───────────────────────────────────────────────────
Write-Step "Step 1 · Restoring NuGet packages"
Push-Location $Root
dotnet restore PixBridge.sln
if ($LASTEXITCODE -ne 0) { Write-Host "dotnet restore failed." -ForegroundColor Red; exit 1 }
Pop-Location

# ── Step 2 · EF Core migrations ──────────────────────────────────────────────
if (-not $SkipMigration) {
    Write-Step "Step 2 · Applying EF Core database migrations"

    # Stop any running PixBridge processes that hold DLL file locks
    $processNames = @('EventPhoto.Api', 'EventPhoto.Worker')
    foreach ($name in $processNames) {
        $procs = Get-Process -Name $name -ErrorAction SilentlyContinue
        if ($procs) {
            Write-Host "  Stopping running process: $name (PID $($procs.Id -join ', '))" -ForegroundColor Yellow
            $procs | Stop-Process -Force
            Start-Sleep -Milliseconds 800
        }
    }

    # Check dotnet-ef tool is available
    $efInstalled = dotnet tool list --global 2>$null | Select-String "dotnet-ef"
    if (-not $efInstalled) {
        Write-Host "  dotnet-ef not found globally — installing..." -ForegroundColor Yellow
        dotnet tool install --global dotnet-ef
    }

    Push-Location $Root
    dotnet ef database update `
        --project  $Infra `
        --startup-project $Api

    if ($LASTEXITCODE -ne 0) {
        Write-Host "`nMigration failed. Common causes:" -ForegroundColor Red
        Write-Host "  • PostgreSQL is not running on localhost:5432"
        Write-Host "  • Database 'pixbridge_dev' does not exist"
        Write-Host "  • Password in appsettings.json does not match"
        Write-Host "`nRe-run with -SkipMigration to skip this step."
        Pop-Location
        exit 1
    }
    Pop-Location
    Write-Host "  Migrations applied successfully." -ForegroundColor Green
} else {
    Write-Host "`n[Skipped] EF Core migration (-SkipMigration flag set)" -ForegroundColor DarkGray
}

# ── Step 3 · npm install ──────────────────────────────────────────────────────
if (-not $SkipNpm) {
    Write-Step "Step 3 · Installing npm packages"
    Push-Location $React
    npm install
    if ($LASTEXITCODE -ne 0) { Write-Host "npm install failed." -ForegroundColor Red; Pop-Location; exit 1 }
    Pop-Location
    Write-Host "  npm packages installed." -ForegroundColor Green
} else {
    Write-Host "`n[Skipped] npm install (-SkipNpm flag set)" -ForegroundColor DarkGray
}

# ── Step 4 (optional) · Build ─────────────────────────────────────────────────
if ($Build) {
    Write-Step "Step 4 · Building .NET solution"
    Push-Location $Root
    dotnet build PixBridge.sln --no-restore --configuration Release
    if ($LASTEXITCODE -ne 0) { Write-Host "dotnet build failed." -ForegroundColor Red; Pop-Location; exit 1 }
    Pop-Location
    Write-Host "  .NET build succeeded." -ForegroundColor Green

    Write-Step "Step 4b · Building React app"
    Push-Location $React
    npm run build
    if ($LASTEXITCODE -ne 0) { Write-Host "React build failed." -ForegroundColor Red; Pop-Location; exit 1 }
    Pop-Location
    Write-Host "  React build succeeded." -ForegroundColor Green
}

# ── Done ──────────────────────────────────────────────────────────────────────
Write-Host "`n✔  Setup complete." -ForegroundColor Green
Write-Host @"

  To start the application:
    API   →  cd src\EventPhoto.Api   && dotnet run
    React →  cd src\EventPhoto.React && npm run dev

"@
