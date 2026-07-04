<#
.SYNOPSIS
    PixBridge Master Deploy Script — one command from source to installer.

.DESCRIPTION
    Runs every step automatically:
      1. Validates prerequisites  (Node, .NET SDK, PostgreSQL, Inno Setup)
      2. Builds React frontend     (npm run build)
      3. Publishes API             (self-contained win-x64 single EXE)
      4. Publishes Worker          (self-contained win-x64 single EXE)
      5. Copies deployment assets  (scripts, docs)
      6. Packages Inno Setup installer  → publish\installer\PixBridge-Setup-<ver>.exe
      7. Prints a ready-to-ship summary

.PARAMETER Version
    Semantic version for this release. Default: 1.0.0

.PARAMETER SkipPrereqCheck
    Skip the prerequisites validation step (useful in CI pipelines).

.PARAMETER SkipInstaller
    Build and publish only — skip Inno Setup packaging.

.EXAMPLE
    # Normal full build
    .\deploy.ps1

.EXAMPLE
    # Specify version
    .\deploy.ps1 -Version "1.2.0"

.EXAMPLE
    # Build only, no installer
    .\deploy.ps1 -SkipInstaller

.NOTES
    Run from the PixBridge repo root.
    Inno Setup 6 must be installed at its default path (or ISCC on PATH).
    PostgreSQL 15+ must be installed and psql must be on PATH.
#>

param(
    [string]$Version      = "1.0.0",
    [switch]$SkipPrereqCheck,
    [switch]$SkipInstaller
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# ─────────────────────────────────────────────
#  Helpers
# ─────────────────────────────────────────────
function Write-Step {
    param([int]$N, [int]$Of, [string]$Msg)
    Write-Host ""
    Write-Host "[$N/$Of] $Msg" -ForegroundColor Cyan
}

function Write-Ok   { param([string]$M) Write-Host "  ✓ $M" -ForegroundColor Green  }
function Write-Warn { param([string]$M) Write-Host "  ⚠ $M" -ForegroundColor Yellow }
function Write-Fail { param([string]$M) Write-Host "  ✗ $M" -ForegroundColor Red    }

function Assert-ExitCode {
    param([string]$Step)
    if ($LASTEXITCODE -ne 0) {
        Write-Fail "$Step failed (exit code $LASTEXITCODE)."
        exit 1
    }
}

function Find-InnoSetupCompiler {
    # Common install paths for Inno Setup 6
    $candidates = @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe",
        "C:\Program Files (x86)\Inno Setup 5\ISCC.exe"
    )
    foreach ($p in $candidates) {
        if (Test-Path $p) { return $p }
    }
    # Fall back to PATH
    $fromPath = Get-Command ISCC -ErrorAction SilentlyContinue
    if ($fromPath) { return $fromPath.Source }
    return $null
}

# ─────────────────────────────────────────────
#  Setup
# ─────────────────────────────────────────────
$Root        = $PSScriptRoot
$PublishRoot = Join-Path $Root "publish"
$ApiOut      = Join-Path $PublishRoot "api"
$WorkerOut   = Join-Path $PublishRoot "worker"
$SetupOut    = Join-Path $PublishRoot "setup"
$InstallerOut= Join-Path $PublishRoot "installer"
$TotalSteps  = if ($SkipInstaller) { 5 } else { 6 }

$StartTime = Get-Date
Write-Host ""
Write-Host "╔══════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║        PixBridge Deploy Script v$Version        ║" -ForegroundColor Magenta
Write-Host "║     Building release for Windows x64...     ║" -ForegroundColor Magenta
Write-Host "╚══════════════════════════════════════════════╝" -ForegroundColor Magenta

# ─────────────────────────────────────────────
#  Step 1 — Prerequisite Check
# ─────────────────────────────────────────────
Write-Step 1 $TotalSteps "Validating prerequisites"

if (-not $SkipPrereqCheck) {
    $allOk = $true

    # Node.js
    $nodeVer = node --version 2>$null
    if ($nodeVer) { Write-Ok "Node.js $nodeVer" }
    else { Write-Fail "Node.js not found. Install from https://nodejs.org"; $allOk = $false }

    # .NET SDK
    $dotnetVer = dotnet --version 2>$null
    if ($dotnetVer) { Write-Ok ".NET SDK $dotnetVer" }
    else { Write-Fail ".NET SDK not found. Install from https://dotnet.microsoft.com"; $allOk = $false }

    # PostgreSQL (psql)
    $psqlVer = psql --version 2>$null
    if ($psqlVer) { Write-Ok "PostgreSQL client: $psqlVer" }
    else { Write-Warn "psql not found in PATH. Installer will warn the user at setup time." }

    # Inno Setup (optional if SkipInstaller)
    if (-not $SkipInstaller) {
        $iscc = Find-InnoSetupCompiler
        if ($iscc) { Write-Ok "Inno Setup: $iscc" }
        else {
            Write-Fail "Inno Setup 6 not found. Install from https://jrsoftware.org/isdl.php"
            Write-Host "         OR re-run with -SkipInstaller to build without packaging." -ForegroundColor Yellow
            $allOk = $false
        }
    }

    if (-not $allOk) {
        Write-Host "`nPrerequisites check failed. Fix the above issues and re-run." -ForegroundColor Red
        exit 1
    }
} else {
    Write-Warn "Prerequisite check skipped (-SkipPrereqCheck)."
}

# ─────────────────────────────────────────────
#  Clean previous publish output
# ─────────────────────────────────────────────
if (Test-Path $PublishRoot) {
    Write-Host "  → Cleaning previous publish output..." -ForegroundColor DarkGray
    Remove-Item $PublishRoot -Recurse -Force
}
New-Item -ItemType Directory -Force -Path $ApiOut      | Out-Null
New-Item -ItemType Directory -Force -Path $WorkerOut   | Out-Null
New-Item -ItemType Directory -Force -Path $SetupOut    | Out-Null
New-Item -ItemType Directory -Force -Path $InstallerOut| Out-Null

# ─────────────────────────────────────────────
#  Step 2 — React Frontend
# ─────────────────────────────────────────────
Write-Step 2 $TotalSteps "Building React frontend (Vite + TypeScript)"

$ReactDir = Join-Path $Root "src\EventPhoto.React"
Set-Location $ReactDir

Write-Host "  → npm ci (clean install)..." -ForegroundColor DarkGray
npm ci --prefer-offline 2>&1 | Where-Object { $_ -match "warn|error" } | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
Assert-ExitCode "npm ci"

Write-Host "  → npm run build..." -ForegroundColor DarkGray
npm run build 2>&1 | Tee-Object -Variable buildOut | Out-Null
Assert-ExitCode "npm run build"

$moduleCount = ($buildOut | Select-String "modules transformed").Line
Write-Ok "React build complete. $moduleCount"

# ─────────────────────────────────────────────
#  Step 3 — Publish API
# ─────────────────────────────────────────────
Write-Step 3 $TotalSteps "Publishing EventPhoto.Api (self-contained win-x64)"

Set-Location $Root
dotnet publish src\EventPhoto.Api\EventPhoto.Api.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:Version=$Version `
    --output $ApiOut `
    --nologo 2>&1 | Where-Object { $_ -match "error|warning|warn" -and $_ -notmatch "0 Error" } | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }

Assert-ExitCode "dotnet publish API"

$apiExeSize = [math]::Round((Get-Item (Join-Path $ApiOut "EventPhoto.Api.exe")).Length / 1MB, 1)
Write-Ok "EventPhoto.Api.exe published ($apiExeSize MB)"

# ─────────────────────────────────────────────
#  Step 4 — Publish Worker
# ─────────────────────────────────────────────
Write-Step 4 $TotalSteps "Publishing EventPhoto.Worker (self-contained win-x64)"

dotnet publish src\EventPhoto.Worker\EventPhoto.Worker.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:Version=$Version `
    --output $WorkerOut `
    --nologo 2>&1 | Where-Object { $_ -match "error|warning|warn" -and $_ -notmatch "0 Error" } | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }

Assert-ExitCode "dotnet publish Worker"

$workerExeSize = [math]::Round((Get-Item (Join-Path $WorkerOut "EventPhoto.Worker.exe")).Length / 1MB, 1)
Write-Ok "EventPhoto.Worker.exe published ($workerExeSize MB)"

# ─────────────────────────────────────────────
#  Step 5 — Copy Deployment Assets
# ─────────────────────────────────────────────
Write-Step 5 $TotalSteps "Copying deployment assets"

$filesToCopy = @(
    @{ Src = "scripts\setup-postgresql.ps1";  Dst = $SetupOut },
    @{ Src = "scripts\install-service.ps1";   Dst = $SetupOut },
    @{ Src = "scripts\uninstall-service.ps1"; Dst = $SetupOut },
    @{ Src = "scripts\installer.iss";         Dst = $SetupOut },
    @{ Src = "docs\README.md";                Dst = $PublishRoot },
    @{ Src = "docs\deployment-guide.md";      Dst = $PublishRoot },
    @{ Src = "PIXBRIDGE.md";                  Dst = $PublishRoot }
)

foreach ($f in $filesToCopy) {
    $src = Join-Path $Root $f.Src
    if (Test-Path $src) {
        Copy-Item $src $f.Dst -Force
        Write-Ok "Copied $(Split-Path $src -Leaf)"
    } else {
        Write-Warn "Not found, skipping: $($f.Src)"
    }
}

# Write a version stamp file
@"
PixBridge Release
Version   : $Version
Built     : $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
Platform  : win-x64 self-contained
.NET      : $(dotnet --version)
Node      : $(node --version)
"@ | Set-Content (Join-Path $PublishRoot "VERSION.txt")
Write-Ok "VERSION.txt written"

# ─────────────────────────────────────────────
#  Step 6 — Package Installer (Inno Setup)
# ─────────────────────────────────────────────
if (-not $SkipInstaller) {
    Write-Step 6 $TotalSteps "Packaging installer with Inno Setup 6"

    $iscc = Find-InnoSetupCompiler
    $issFile = Join-Path $Root "scripts\installer.iss"

    # Patch version into the .iss file temporarily
    $issContent = Get-Content $issFile -Raw
    $issPatched = $issContent -replace '#define MyAppVersion "[^"]*"', "#define MyAppVersion `"$Version`""
    $issTemp    = Join-Path $env:TEMP "pixbridge_installer_$Version.iss"
    $issPatched | Set-Content $issTemp -Encoding UTF8

    Write-Host "  → Running ISCC..." -ForegroundColor DarkGray
    & $iscc $issTemp /O"$InstallerOut" /F"PixBridge-Setup-$Version" 2>&1 | ForEach-Object {
        if ($_ -match "error|Error") { Write-Host "    $_" -ForegroundColor Red }
    }
    Assert-ExitCode "Inno Setup compile"

    Remove-Item $issTemp -Force

    $installerFile = Join-Path $InstallerOut "PixBridge-Setup-$Version.exe"
    if (Test-Path $installerFile) {
        $installerSize = [math]::Round((Get-Item $installerFile).Length / 1MB, 1)
        Write-Ok "Installer created: PixBridge-Setup-$Version.exe ($installerSize MB)"
    }
}

# ─────────────────────────────────────────────
#  Summary
# ─────────────────────────────────────────────
$Elapsed = [math]::Round(((Get-Date) - $StartTime).TotalSeconds)

Write-Host ""
Write-Host "╔══════════════════════════════════════════════╗" -ForegroundColor Green
Write-Host "║          ✅  BUILD COMPLETE                  ║" -ForegroundColor Green
Write-Host "╚══════════════════════════════════════════════╝" -ForegroundColor Green
Write-Host ""
Write-Host "  Version  : $Version"                                                   -ForegroundColor White
Write-Host "  Duration : ${Elapsed}s"                                                -ForegroundColor White
Write-Host "  Output   : $PublishRoot"                                               -ForegroundColor White
Write-Host ""
Write-Host "  📦 Artifacts:"                                                         -ForegroundColor Yellow
Write-Host "     API EXE    → publish\api\EventPhoto.Api.exe"                        -ForegroundColor Gray
Write-Host "     Worker EXE → publish\worker\EventPhoto.Worker.exe"                  -ForegroundColor Gray
if (-not $SkipInstaller) {
Write-Host "     Installer  → publish\installer\PixBridge-Setup-$Version.exe  ← ship this" -ForegroundColor Green
}
Write-Host ""
Write-Host "  📋 Next steps for client machine:"                                     -ForegroundColor Yellow
Write-Host "     1. Install PostgreSQL 15+  (https://www.postgresql.org/download/windows/)" -ForegroundColor Gray
Write-Host "     2. Run  PixBridge-Setup-$Version.exe  as Administrator"             -ForegroundColor Gray
Write-Host "     3. Click Next → Next → Finish"                                      -ForegroundColor Gray
Write-Host "     4. Open browser → http://localhost:5000/admin"                      -ForegroundColor Gray
Write-Host "     5. Login: admin / Admin@1234!"                                      -ForegroundColor Gray
Write-Host ""
Write-Host "  ⚠  SECURITY reminders before going live:"                              -ForegroundColor Yellow
Write-Host "     • Change the default admin password"                                -ForegroundColor Gray
Write-Host "     • Override JWT secret via environment variable"                     -ForegroundColor Gray
Write-Host "     • Override DB password in appsettings.Production.json"              -ForegroundColor Gray
Write-Host ""
