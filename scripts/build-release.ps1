<#
.SYNOPSIS
    Builds PixBridge for production release and produces a single setup .exe.
.DESCRIPTION
    Full pipeline — one script, one output:

    PRE-BUILD CHECKS
      • appsettings.Production.json has a real JWT secret
      • TypeScript compiles with zero errors
      • .NET solution builds with zero errors

    BUILD
      1. React frontend  → EventPhoto.Api/wwwroot
      2. EventPhoto.Api  → publish/api/   (self-contained win-x64 exe)
      3. EventPhoto.Worker → publish/worker/ (self-contained win-x64 exe)
      4. Deployment assets → publish/setup/

    POST-BUILD VERIFICATION
      • All expected output files present
      • API exe size sanity check

    INSTALLER
      • Auto-installs Inno Setup 6 via winget if not already present
      • Compiles installer.iss → publish/installer/PixBridge-Setup-<version>.exe
      • That single .exe is everything needed for a new machine
.PARAMETER Version
    Version number embedded in the installer filename. Default: 1.0.0
.EXAMPLE
    .\build-release.ps1
    .\build-release.ps1 -Version "2.1.0"
#>

param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$Root        = Split-Path $PSScriptRoot -Parent
$PublishRoot = Join-Path $Root "publish"

function Pass  { param([string]$Msg) Write-Host "  [PASS] $Msg" -ForegroundColor Green }
function Fail  { param([string]$Msg) Write-Host "  [FAIL] $Msg" -ForegroundColor Red }
function Check { param([string]$Msg) Write-Host "  [CHECK] $Msg" -ForegroundColor Cyan }

Write-Host "`n===== PixBridge Release Build v$Version =====" -ForegroundColor Cyan

# ─────────────────────────────────────────────────────────────────────────────
# PREREQUISITES — must be installed on this machine before anything else
# ─────────────────────────────────────────────────────────────────────────────
Write-Host "`n[PREREQUISITES] Checking required tools..." -ForegroundColor Yellow
$prereqFailed = $false

# .NET SDK 8+
$dotnetVer = dotnet --version 2>&1
if ($LASTEXITCODE -ne 0 -or -not ($dotnetVer -match '^8\.|^9\.')) {
    Write-Host "  [FAIL] .NET SDK 8 or 9 is required. Found: $dotnetVer" -ForegroundColor Red
    Write-Host "         Download: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    $prereqFailed = $true
} else {
    Pass ".NET SDK $dotnetVer"
}

# Node.js 18+
$nodeVer = node --version 2>&1
$nodeMajor = if ($nodeVer -match 'v(\d+)') { [int]$Matches[1] } else { 0 }
if ($LASTEXITCODE -ne 0 -or $nodeMajor -lt 18) {
    Write-Host "  [FAIL] Node.js 18 or higher is required. Found: $nodeVer" -ForegroundColor Red
    Write-Host "         Download: https://nodejs.org/en/download/" -ForegroundColor Yellow
    $prereqFailed = $true
} else {
    Pass "Node.js $nodeVer"
}

if ($prereqFailed) {
    Write-Host "`n[PREREQUISITES FAILED] Install the missing tools above and rerun.`n" -ForegroundColor Red
    exit 1
}

# ─────────────────────────────────────────────────────────────────────────────
# PRE-BUILD CHECKS — all must pass before a single file is compiled
# ─────────────────────────────────────────────────────────────────────────────
Write-Host "`n[PRE-BUILD] Running checks..." -ForegroundColor Yellow

$failed = $false

# ── Check 1: appsettings.Production.json exists ───────────────────────────
Check "appsettings.Production.json exists"
$prodSettings = Join-Path $Root "src\EventPhoto.Api\appsettings.Production.json"
if (-not (Test-Path $prodSettings)) {
    Fail "src\EventPhoto.Api\appsettings.Production.json not found."
    Write-Host "         Create it from appsettings.json and set Jwt:Secret." -ForegroundColor Yellow
    $failed = $true
} else {
    Pass "appsettings.Production.json found."
}

# ── Check 2: JWT secret is not blank or placeholder ───────────────────────
Check "JWT secret is set"
if (-not $failed) {
    $settingsJson = Get-Content $prodSettings -Raw | ConvertFrom-Json
    $secret = $settingsJson.Jwt.Secret
    if ([string]::IsNullOrWhiteSpace($secret)) {
        Fail "Jwt.Secret is empty in appsettings.Production.json."
        $failed = $true
    } elseif ($secret -match "CHANGE-THIS|placeholder|your.*secret|example|changeme" -or $secret.Length -lt 32) {
        Fail "Jwt.Secret looks like a placeholder or is shorter than 32 characters."
        Write-Host "         Current value: '$secret'" -ForegroundColor Yellow
        Write-Host "         Set a real random secret of at least 32 characters." -ForegroundColor Yellow
        $failed = $true
    } else {
        Pass "JWT secret is set ($($secret.Length) chars)."
    }
}

# ── Check 3: DB connection string is not pointing at pixbridge_dev ─────────
Check "Production DB name is not 'pixbridge_dev'"
if (-not $failed -or (Test-Path $prodSettings)) {
    try {
        $dbConn = $settingsJson.ConnectionStrings.DefaultConnection
        if ($dbConn -match "pixbridge_dev") {
            Fail "ConnectionString still points to 'pixbridge_dev' (dev database)."
            Write-Host "         Change Database= to 'pixbridge' in appsettings.Production.json." -ForegroundColor Yellow
            $failed = $true
        } else {
            Pass "DB connection string looks correct."
        }
    } catch { }
}

# ── Check 4: TypeScript — zero type errors ────────────────────────────────
Check "TypeScript type check (tsc --noEmit)"
$reactDir = Join-Path $Root "src\EventPhoto.React"
Push-Location $reactDir
$tscOut = npx tsc --noEmit 2>&1
$tscExit = $LASTEXITCODE
Pop-Location
if ($tscExit -ne 0) {
    Fail "TypeScript errors found:"
    $tscOut | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
    $failed = $true
} else {
    Pass "TypeScript: no errors."
}

# ── Check 5: .NET solution builds cleanly ─────────────────────────────────
Check ".NET solution builds (dotnet build -c Release)"
Set-Location $Root
$buildOut = dotnet build PixBridge.sln -c Release --nologo -v quiet 2>&1
$buildExit = $LASTEXITCODE
if ($buildExit -ne 0) {
    Fail ".NET build errors found:"
    $buildOut | Where-Object { $_ -match "error|Error" } | ForEach-Object { Write-Host "    $_" -ForegroundColor Red }
    $failed = $true
} else {
    Pass ".NET solution: builds clean."
}

# ── Abort if any check failed ─────────────────────────────────────────────
if ($failed) {
    Write-Host "`n[PRE-BUILD FAILED] Fix the issues above and re-run the build.`n" -ForegroundColor Red
    exit 1
}

Write-Host "`n[PRE-BUILD] All checks passed. Starting build...`n" -ForegroundColor Green

# ─────────────────────────────────────────────────────────────────────────────
# BUILD
# ─────────────────────────────────────────────────────────────────────────────

# Step 1: Build React frontend
Write-Host "[1/4] Building React frontend..." -ForegroundColor Yellow
Set-Location (Join-Path $Root "src\EventPhoto.React")

# Install npm dependencies if node_modules is missing or outdated
if (-not (Test-Path "node_modules")) {
    Write-Host "  node_modules not found — running npm ci..." -ForegroundColor Cyan
    npm ci
    if ($LASTEXITCODE -ne 0) { throw "npm ci failed." }
}

npm run build
if ($LASTEXITCODE -ne 0) { throw "React build failed." }

# Step 2: Publish API
Write-Host "`n[2/4] Publishing EventPhoto.Api..." -ForegroundColor Yellow
$ApiPublish = Join-Path $PublishRoot "api"
Set-Location $Root
dotnet publish src\EventPhoto.Api\EventPhoto.Api.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:EnvironmentName=Production `
    -p:Version=$Version `
    -o $ApiPublish

if ($LASTEXITCODE -ne 0) { throw "API publish failed." }

# Step 3: Publish Worker
Write-Host "`n[3/4] Publishing EventPhoto.Worker..." -ForegroundColor Yellow
$WorkerPublish = Join-Path $PublishRoot "worker"
dotnet publish src\EventPhoto.Worker\EventPhoto.Worker.csproj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:PublishReadyToRun=true `
    -p:EnvironmentName=Production `
    -p:Version=$Version `
    -o $WorkerPublish

if ($LASTEXITCODE -ne 0) { throw "Worker publish failed." }

# Step 4: Copy shared config files and Face Recognition source
Write-Host "`n[4/4] Copying deployment assets..." -ForegroundColor Yellow
$SetupDir = Join-Path $PublishRoot "setup"
New-Item -ItemType Directory -Force -Path $SetupDir | Out-Null
Copy-Item (Join-Path $Root "scripts\setup-postgresql.ps1") $SetupDir -Force
Copy-Item (Join-Path $Root "scripts\install-service.ps1") $SetupDir -Force
Copy-Item (Join-Path $Root "scripts\uninstall-service.ps1") $SetupDir -Force
Copy-Item (Join-Path $Root "scripts\setup-face-search.ps1") $SetupDir -Force
Copy-Item (Join-Path $Root "scripts\fix-network-access.ps1") $SetupDir -Force
Copy-Item (Join-Path $Root "scripts\repair.ps1") $SetupDir -Force
Copy-Item (Join-Path $Root "scripts\installer.iss") $SetupDir -Force
Copy-Item (Join-Path $Root "docs\README.md") $PublishRoot -Force
Copy-Item (Join-Path $Root "docs\deployment-guide.md") $PublishRoot -Force

# Copy Face Recognition Python service
$FaceRecDir = Join-Path $PublishRoot "face-recognition"
New-Item -ItemType Directory -Force -Path $FaceRecDir | Out-Null
Copy-Item (Join-Path $Root "src\PixBridge.FaceRecognition\*") $FaceRecDir -Recurse -Force

# ─────────────────────────────────────────────────────────────────────────────
# POST-BUILD VERIFICATION
# ─────────────────────────────────────────────────────────────────────────────
Write-Host "`n[POST-BUILD] Verifying output..." -ForegroundColor Yellow

$verifyFailed = $false

$checks = @{
    "API executable"              = Join-Path $ApiPublish    "EventPhoto.Api.exe"
    "Worker executable"           = Join-Path $WorkerPublish "EventPhoto.Worker.exe"
    "API production settings"     = Join-Path $ApiPublish    "appsettings.Production.json"
    "Worker production settings"  = Join-Path $WorkerPublish "appsettings.Production.json"
    "React wwwroot index.html"    = Join-Path $ApiPublish    "wwwroot\index.html"
    "setup-postgresql.ps1"        = Join-Path $SetupDir      "setup-postgresql.ps1"
    "install-service.ps1"         = Join-Path $SetupDir      "install-service.ps1"
    "repair.ps1"                  = Join-Path $SetupDir      "repair.ps1"
}

foreach ($item in $checks.GetEnumerator()) {
    if (Test-Path $item.Value) {
        Pass $item.Key
    } else {
        Fail "$($item.Key) — missing: $($item.Value)"
        $verifyFailed = $true
    }
}

# API exe size sanity check (self-contained should be > 20 MB)
$apiExe = Get-Item (Join-Path $ApiPublish "EventPhoto.Api.exe") -ErrorAction SilentlyContinue
if ($apiExe -and $apiExe.Length -lt 20MB) {
    Fail "EventPhoto.Api.exe is suspiciously small ($([math]::Round($apiExe.Length/1MB,1)) MB). Build may be incomplete."
    $verifyFailed = $true
} elseif ($apiExe) {
    Pass "API exe size: $([math]::Round($apiExe.Length/1MB,1)) MB"
}

if ($verifyFailed) {
    Write-Host "`n[POST-BUILD FAILED] Some expected output files are missing.`n" -ForegroundColor Red
    exit 1
}

Write-Host "`n===== Build complete! Output: $PublishRoot =====" -ForegroundColor Green
Write-Host "API:            $ApiPublish"
Write-Host "Worker:         $WorkerPublish"
Write-Host "Face Rec:       $FaceRecDir"
Write-Host "Setup scripts:  $SetupDir"

# ─────────────────────────────────────────────────────────────────────────────
# INSTALLER — auto-install Inno Setup if missing, then compile to .exe
# ─────────────────────────────────────────────────────────────────────────────
Write-Host "`n[INSTALLER] Locating Inno Setup..." -ForegroundColor Yellow

function Find-Iscc {
    return @(
        "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
        "C:\Program Files\Inno Setup 6\ISCC.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
}

$iscc = Find-Iscc

if (-not $iscc) {
    Write-Host "  Inno Setup not found. Attempting to install..." -ForegroundColor Yellow

    function Install-InnoSetup {
        # Try 1: winget
        $winget = Get-Command winget -ErrorAction SilentlyContinue
        if ($winget) {
            Write-Host "  [1/3] Trying winget..." -ForegroundColor Cyan
            winget install --id JRSoftware.InnoSetup --accept-source-agreements --accept-package-agreements --silent 2>&1 | Out-Null
            if ($LASTEXITCODE -eq 0) { return $true }
            Write-Host "        winget failed, trying direct download..." -ForegroundColor Yellow
        }

        # Try 2: Direct download from jrsoftware.org
        $urls = @(
            "https://files.jrsoftware.org/is/6/innosetup-6.3.3.exe",
            "https://github.com/jrsoftware/issrc/releases/download/is-6_3_3/innosetup-6.3.3.exe"
        )
        $dest = "$env:TEMP\innosetup-installer.exe"
        foreach ($url in $urls) {
            Write-Host "  [2/3] Downloading from $url ..." -ForegroundColor Cyan
            try {
                [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
                $wc = New-Object System.Net.WebClient
                $wc.DownloadFile($url, $dest)
                if ((Get-Item $dest -ErrorAction SilentlyContinue).Length -gt 1MB) {
                    Write-Host "        Download OK. Installing silently..." -ForegroundColor Cyan
                    Start-Process $dest -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART" -Wait
                    return $true
                }
            } catch {
                Write-Host "        Failed: $_" -ForegroundColor Yellow
            }
        }

        return $false
    }

    $installed = Install-InnoSetup

    # Refresh PATH after install
    $env:PATH = [System.Environment]::GetEnvironmentVariable('PATH','Machine') + ';' +
                [System.Environment]::GetEnvironmentVariable('PATH','User')
    $iscc = Find-Iscc
    if ($iscc) { Pass "Inno Setup installed: $iscc" }

    if (-not $iscc) {
        Write-Host "" 
        Write-Host "  [3/3] Auto-install failed (no internet access or blocked)." -ForegroundColor Yellow
        Write-Host "        Manual options:" -ForegroundColor Yellow
        Write-Host "          A) Download on another machine: https://jrsoftware.org/isdl.php" -ForegroundColor Cyan
        Write-Host "             Copy innosetup-6.x.x.exe here, run it, then rerun build-release.ps1" -ForegroundColor Cyan
        Write-Host "          B) Use zip deployment instead (no .exe, but works):" -ForegroundColor Cyan
        Write-Host "             Compress-Archive publish\* -DestinationPath PixBridge-$Version.zip" -ForegroundColor Cyan
    }
} else {
    Pass "Inno Setup found: $iscc"
}

if ($iscc) {
    Write-Host "`n[INSTALLER] Compiling installer..." -ForegroundColor Yellow
    $issFile = Join-Path $Root "scripts\installer.iss"

    # Update version in .iss before compile
    $issContent = Get-Content $issFile -Raw
    $issContent = $issContent -replace '#define MyAppVersion "[^"]+"', "#define MyAppVersion `"$Version`""
    Set-Content $issFile -Value $issContent -Encoding UTF8

    & $iscc $issFile
    if ($LASTEXITCODE -ne 0) {
        Write-Host "`n[INSTALLER FAILED] Inno Setup compile failed." -ForegroundColor Red
        exit 1
    }

    $installerExe = Join-Path $PublishRoot "installer\PixBridge-Setup-$Version.exe"
    if (Test-Path $installerExe) {
        $sizeMb = [math]::Round((Get-Item $installerExe).Length / 1MB, 1)
        Write-Host ""
        Write-Host "╔══════════════════════════════════════════════════════╗" -ForegroundColor Green
        Write-Host "║  INSTALLER READY                                     ║" -ForegroundColor Green
        Write-Host "║                                                      ║" -ForegroundColor Green
        Write-Host "║  $installerExe" -ForegroundColor Green
        Write-Host "║  Size: $sizeMb MB                                          ║" -ForegroundColor Green
        Write-Host "║                                                      ║" -ForegroundColor Green
        Write-Host "║  Copy this single file to the target machine         ║" -ForegroundColor Green
        Write-Host "║  Run as Administrator to install PixBridge           ║" -ForegroundColor Green
        Write-Host "╚══════════════════════════════════════════════════════╝" -ForegroundColor Green
    } else {
        Fail "Installer exe not found at expected path: $installerExe"
        exit 1
    }
} else {
    Write-Host "`n[INSTALLER SKIPPED] Inno Setup could not be installed automatically." -ForegroundColor Yellow
    Write-Host "  Manual option — zip the publish folder instead:" -ForegroundColor Yellow
    Write-Host "  Compress-Archive publish\* -DestinationPath PixBridge-$Version.zip" -ForegroundColor Yellow
    Write-Host "  Then extract on target machine and run setup scripts manually." -ForegroundColor Yellow
}
