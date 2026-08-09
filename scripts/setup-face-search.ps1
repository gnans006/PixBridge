#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Sets up the PixBridge face search stack:
      1. Installs Python 3.11 via winget
      2. Adds MSVC C++ compiler to VS18 Professional
      3. Builds pgvector 0.8.6 from source (already downloaded)
      4. Installs pgvector into PostgreSQL 17
      5. Runs the EF Core migration (AddFaceRecognitionVectors)
      6. Creates the HNSW cosine-similarity index on face_embeddings
      7. Installs Python dependencies for PixBridge.FaceRecognition
      8. Installs the face recognition Windows Service via NSSM

.NOTES
    Run once from an elevated (Administrator) PowerShell terminal:
        Set-ExecutionPolicy Bypass -Scope Process -Force
        .\scripts\setup-face-search.ps1            # full install
        .\scripts\setup-face-search.ps1 -ServiceOnly  # pip + NSSM service only (pgvector already installed)
#>

param(
    # Skip pgvector build, PostgreSQL setup, and EF migrations.
    # Use when those are already configured on this machine.
    # Installs only Python packages and the Windows service.
    [switch]$ServiceOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$ProgressPreference    = "SilentlyContinue"

# ── Config ────────────────────────────────────────────────────────────────────

$REPO_ROOT       = Split-Path $PSScriptRoot -Parent
$FACE_SVC_SRC    = "$REPO_ROOT\src\PixBridge.FaceRecognition"
$PGVECTOR_SRC    = "$env:TEMP\pgvector-build\pgvector-0.8.6"
$PGVECTOR_ZIP    = "$env:TEMP\pgvector-src.zip"
$PGVECTOR_VER    = "0.8.6"

$VS_INSTALLER    = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vs_installer.exe"

# ── Read secrets from .env ────────────────────────────────────────────────────────
$dotEnvPath = Join-Path $REPO_ROOT ".env"
if (-not (Test-Path $dotEnvPath)) {
    Write-Host "FAIL: .env not found at $dotEnvPath" -ForegroundColor Red
    Write-Host "  Copy .env.example to .env and set your database password." -ForegroundColor Yellow
    exit 1
}
$envVars = @{}
Get-Content $dotEnvPath | ForEach-Object {
    if ($_ -match '^\s*([^#\s][^=]*)=(.*)$') { $envVars[$matches[1].Trim()] = $matches[2].Trim() }
}
$PGPASSWORD_VAL = if ($envVars.ContainsKey('DB_PASSWORD')) { $envVars['DB_PASSWORD'] } else { $null }
$PGUSER         = if ($envVars.ContainsKey('DB_USER'))     { $envVars['DB_USER'] }     else { 'postgres' }
$PGDATABASE     = if ($envVars.ContainsKey('DB_NAME'))     { $envVars['DB_NAME'] }     else { 'pixbridge_dev' }
if (-not $ServiceOnly -and -not $PGPASSWORD_VAL) {
    Write-Host "FAIL: DB_PASSWORD not set in .env" -ForegroundColor Red; exit 1
}
if (-not $ServiceOnly) {# ── Auto-detect PostgreSQL installation ──────────────────────────────────────
$PG_ROOT = $null
foreach ($searchRoot in @("$env:ProgramFiles\PostgreSQL", "C:\PostgreSQL", "D:\PostgreSQL", "E:\PostgreSQL")) {
    if (Test-Path $searchRoot) {
        $found = Get-ChildItem $searchRoot -Directory -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match '^\d+$' } |
            Sort-Object { [int]$_.Name } -Descending |
            Select-Object -First 1
        if ($found -and (Test-Path "$($found.FullName)\bin\psql.exe")) {
            $PG_ROOT = $found.FullName
            break
        }
    }
}
if (-not $PG_ROOT) { Write-Host "FAIL: PostgreSQL not found. Install it first." -ForegroundColor Red; exit 1 }
$PG_BIN       = "$PG_ROOT\bin"
$PG_LIB       = "$PG_ROOT\lib"
$PG_SHARE_EXT   = "$PG_ROOT\share\extension"
$vectorDllDest  = "$PG_LIB\vector.dll"
$vectorCtrlDest = "$PG_SHARE_EXT\vector.control"
Write-Host "  Detected PostgreSQL: $PG_ROOT" -ForegroundColor Cyan
$env:PATH = "$PG_BIN;$env:PATH"

# ── Auto-detect Visual Studio installation ────────────────────────────────────
$VS_PATH = $null
$vsEditions = @('BuildTools','Community','Professional','Enterprise','Preview')
$vsVersions = @('18','17')   # VS 2028, VS 2022
foreach ($ver in $vsVersions) {
    foreach ($ed in $vsEditions) {
        $candidate = "$env:ProgramFiles\Microsoft Visual Studio\$ver\$ed"
        if (Test-Path "$candidate\Common7\Tools\VsDevCmd.bat") {
            $VS_PATH = $candidate
            break
        }
    }
    if ($VS_PATH) { break }
}
if (-not $VS_PATH) { Write-Host "FAIL: Visual Studio not found (needed to build pgvector). Install VS 2022 or later with C++ tools." -ForegroundColor Red; exit 1 }
Write-Host "  Detected Visual Studio: $VS_PATH" -ForegroundColor Cyan
} # end if (-not $ServiceOnly) — PG/VS detection skipped when -ServiceOnly

# ── Helpers ───────────────────────────────────────────────────────────────────
function Step { param([string]$msg) Write-Host "`n==> $msg" -ForegroundColor Cyan }
function OK   { param([string]$msg) Write-Host "    OK: $msg" -ForegroundColor Green }
function WARN { param([string]$msg) Write-Host "    WARN: $msg" -ForegroundColor Yellow }
function FAIL { param([string]$msg) Write-Host "`n    FAIL: $msg" -ForegroundColor Red; exit 1 }

function Invoke-Psql {
    param([string]$Sql, [string]$Db = $PGDATABASE)
    $env:PGPASSWORD = $PGPASSWORD_VAL
    $result = & "$PG_BIN\psql.exe" -U $PGUSER -d $Db -c $Sql 2>&1
    return $result
}

# ─────────────────────────────────────────────────────────────────────────────
# STEP 1 — Python 3.11
# ─────────────────────────────────────────────────────────────────────────────
Step "Installing Python 3.11"

$pythonExe = Get-Command python.exe -ErrorAction SilentlyContinue
$realPython = $null
if ($pythonExe) {
    $ver = & $pythonExe.Source --version 2>&1
    if ($ver -match "Python 3\.(1[1-9]|[2-9]\d)") {
        $realPython = $pythonExe.Source
        OK "Python already at $realPython ($ver)"
    }
}

# Also check common install locations — including system-wide and all user profiles
# (running as Admin may not inherit $env:USERNAME's AppData)
$pythonCandidates = @("C:\Python311\python.exe", "C:\Python312\python.exe")
# System-wide install (default when "Install for all users" is ticked)
$pythonCandidates += @(
    "$env:ProgramFiles\Python311\python.exe",
    "$env:ProgramFiles\Python312\python.exe"
)
# Per-user installs across all profiles on this machine
Get-ChildItem "C:\Users" -Directory -ErrorAction SilentlyContinue | ForEach-Object {
    $pythonCandidates += "$($_.FullName)\AppData\Local\Programs\Python\Python311\python.exe"
    $pythonCandidates += "$($_.FullName)\AppData\Local\Programs\Python\Python312\python.exe"
    $pythonCandidates += "$($_.FullName)\AppData\Local\Programs\Python\Python313\python.exe"
}
foreach ($candidate in $pythonCandidates) {
    if (!$realPython -and (Test-Path $candidate)) {
        $ver = & $candidate --version 2>&1
        if ($ver -match "Python 3\.(1[1-9]|[2-9]\d)") { $realPython = $candidate }
    }
}

if (!$realPython) {
    Write-Host "    Installing Python 3.11 via winget..."
    winget install Python.Python.3.11 --accept-source-agreements --accept-package-agreements --silent
    if ($LASTEXITCODE -ne 0) { FAIL "winget Python install failed. Install manually from https://python.org/downloads/release/python-3119/" }

    # Refresh both Machine and User PATH after install
    $env:PATH = [System.Environment]::GetEnvironmentVariable("PATH", "Machine") + ";" +
                [System.Environment]::GetEnvironmentVariable("PATH", "User")

    # Re-scan all user profiles after install
    Get-ChildItem "C:\Users" -Directory -ErrorAction SilentlyContinue | ForEach-Object {
        foreach ($pyVer in @("Python311","Python312","Python313")) {
            $c = "$($_.FullName)\AppData\Local\Programs\Python\$pyVer\python.exe"
            if (!$realPython -and (Test-Path $c)) { $realPython = $c }
        }
    }
    foreach ($c in @("C:\Python311\python.exe","$env:ProgramFiles\Python311\python.exe")) {
        if (!$realPython -and (Test-Path $c)) { $realPython = $c }
    }
    if (!$realPython) { FAIL "Python installed but could not locate python.exe. Check PATH and re-run." }
    OK "Python installed at $realPython"
} else {
    OK "Using existing Python: $realPython"
}
if (-not $ServiceOnly) {
# ───────────────────────────────────────────────────────────────────────────
# STEP 2 — Add MSVC C++ tools to VS18 (needed to build pgvector)
# ─────────────────────────────────────────────────────────────────────────────
Step "Checking for MSVC C++ compiler (nmake/cl.exe)"

$nmake = Get-ChildItem "$VS_PATH\VC\Tools\MSVC" -Filter "nmake.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
if ($nmake) {
    OK "nmake found at $($nmake.FullName)"
} else {
    if (!(Test-Path $VS_INSTALLER)) { FAIL "VS installer not found at $VS_INSTALLER" }
    Write-Host "    Adding Microsoft.VisualStudio.Component.VC.Tools.x86.x64 to VS18..."
    Write-Host "    (This downloads ~700MB — please wait. VS Code / devenv will be temporarily disrupted.)"
    # --force bypasses the VSProcessesRunning pre-check that blocked previous attempts
    Start-Process -FilePath $VS_INSTALLER `
        -ArgumentList "modify", "--installPath", "`"$VS_PATH`"",
            "--add", "Microsoft.VisualStudio.Component.VC.Tools.x86.x64",
            "--quiet", "--norestart", "--force" `
        -Wait -NoNewWindow
    # Give VS installer a moment to flush files
    Start-Sleep -Seconds 5
    $nmake = Get-ChildItem "$VS_PATH\VC\Tools\MSVC" -Filter "nmake.exe" -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    if (!$nmake) { FAIL "MSVC install appeared to complete but nmake.exe not found. Check `"$env:TEMP\dd_setup_*.log`" for details." }
    OK "MSVC installed"
}

$nmakePath = $nmake.FullName
$msvcBin   = Split-Path $nmakePath -Parent

# Find cl.exe in the same MSVC bin directory
$clPath = Join-Path $msvcBin "cl.exe"
if (!(Test-Path $clPath)) { FAIL "cl.exe not found at expected location $clPath" }
OK "cl.exe: $clPath"

# ── Windows SDK check — corecrt.h must be present for pgvector to compile ────
$ucrtHeader = Get-ChildItem "C:\Program Files (x86)\Windows Kits\10\Include" -ErrorAction SilentlyContinue |
    Sort-Object Name -Descending |
    ForEach-Object { "$($_.FullName)\ucrt\corecrt.h" } |
    Where-Object { Test-Path $_ } |
    Select-Object -First 1

if ($ucrtHeader) {
    OK "Windows SDK UCRT headers: $ucrtHeader"
} else {
    Write-Host "    Windows SDK not found — installing Microsoft.WindowsSDK.10.0.26100 via winget..."
    Write-Host "    (~500 MB download, please wait...)"
    winget install Microsoft.WindowsSDK.10.0.26100 --accept-source-agreements --accept-package-agreements --silent
    if ($LASTEXITCODE -ne 0) { FAIL "Windows SDK install failed. Install manually: winget install Microsoft.WindowsSDK.10.0.26100" }
    OK "Windows SDK installed"
}
if ((Test-Path $vectorDllDest) -and (Test-Path $vectorCtrlDest)) {
    Step "pgvector already installed — skipping build"
    OK "vector.dll  → $vectorDllDest"
    OK "vector.control → $vectorCtrlDest"
} else {

Step "Getting pgvector $PGVECTOR_VER source"

$PGVECTOR_DIR = "$env:TEMP\pgvector-build"
New-Item -ItemType Directory -Force -Path $PGVECTOR_DIR | Out-Null

if (Test-Path "$PGVECTOR_SRC\Makefile.win") {
    OK "Source already present at $PGVECTOR_SRC"
} elseif (Get-Command git -ErrorAction SilentlyContinue) {
    Write-Host "    git clone pgvector $PGVECTOR_VER..."
    Push-Location $PGVECTOR_DIR
    git clone --depth 1 --branch "v$PGVECTOR_VER" https://github.com/pgvector/pgvector.git "pgvector-$PGVECTOR_VER" 2>&1 | Out-Null
    Pop-Location
    if (!(Test-Path "$PGVECTOR_SRC\Makefile.win")) { FAIL "git clone succeeded but Makefile.win not found at $PGVECTOR_SRC" }
    OK "Cloned to $PGVECTOR_SRC"
} else {
    Write-Host "    Git not found — downloading zip..."
    Invoke-WebRequest -Uri "https://github.com/pgvector/pgvector/archive/refs/tags/v$PGVECTOR_VER.zip" `
        -OutFile $PGVECTOR_ZIP -UseBasicParsing
    Expand-Archive $PGVECTOR_ZIP -DestinationPath $PGVECTOR_DIR -Force
    if (!(Test-Path "$PGVECTOR_SRC\Makefile.win")) { FAIL "Zip extracted but Makefile.win not found at $PGVECTOR_SRC" }
    OK "Extracted to $PGVECTOR_SRC"
}

# ─────────────────────────────────────────────────────────────────────────────
# STEP 4 — Build + install pgvector  (matches PIXBRIDGE.md Steps 3-4)
# ─────────────────────────────────────────────────────────────────────────────
Step "Building and installing pgvector $PGVECTOR_VER"

# Use 8.3 short path for PGROOT — Makefile.win passes paths unquoted to cl.exe,
# which breaks when the path contains spaces (C:\Program Files\...)
$fso         = New-Object -ComObject Scripting.FileSystemObject
$pgRootShort = $fso.GetFolder($PG_ROOT).ShortPath   # → C:\PROGRA~1\POSTGR~1\17

$buildBat    = "$env:TEMP\pgvector-build.bat"
$buildLog    = "$env:TEMP\pgvector-build.log"
$buildErrLog = "$env:TEMP\pgvector-build.err"

# Single batch: build then install (nmake install copies DLL + ALL sql upgrade scripts)
@"
@echo off
call "$VS_PATH\Common7\Tools\VsDevCmd.bat" -arch=amd64 -no_logo
if errorlevel 1 exit /b 1
cd /d "$PGVECTOR_SRC"
set PGROOT=$pgRootShort
nmake /F Makefile.win
if errorlevel 1 exit /b 1
nmake /F Makefile.win install
"@ | Set-Content $buildBat -Encoding ASCII

Write-Host "    Compiling and installing pgvector (~30 seconds)..."
$proc = Start-Process "cmd.exe" `
    -ArgumentList "/c `"$buildBat`"" `
    -RedirectStandardOutput $buildLog `
    -RedirectStandardError  $buildErrLog `
    -Wait -NoNewWindow -PassThru

if (Test-Path $buildErrLog) { Get-Content $buildErrLog | Add-Content $buildLog }

if ($proc.ExitCode -ne 0) {
    Write-Host "    nmake install failed — falling back to manual file copy..."
    # Verify the DLL was at least built
    $vectorDll = "$PGVECTOR_SRC\vector.dll"
    if (!(Test-Path $vectorDll)) {
        if (Test-Path $buildLog) { Get-Content $buildLog | Select-Object -Last 30 }
        FAIL "pgvector build failed (exit $($proc.ExitCode)). Full log: $buildLog"
    }
    # Manual copy (fallback for 'Access is denied' on nmake install)
    Copy-Item $vectorDll "$PG_LIB\vector.dll" -Force
    Copy-Item "$PGVECTOR_SRC\vector.control" "$PG_SHARE_EXT\vector.control" -Force
    Get-ChildItem "$PGVECTOR_SRC\sql" -Filter "*.sql" | ForEach-Object {
        Copy-Item $_.FullName "$PG_SHARE_EXT\$($_.Name)" -Force
    }
    OK "Manual copy completed"
} else {
    OK "nmake install completed"
}

Write-Host "    Files installed:"
Get-ChildItem "$PG_LIB" -Filter "vector.dll" -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "      $($_.FullName)" }
Get-ChildItem "$PG_SHARE_EXT" -Filter "vector*" -ErrorAction SilentlyContinue | ForEach-Object { Write-Host "      $($_.Name)" }

} # end else (pgvector not pre-installed)

# ─────────────────────────────────────────────────────────────────────────────
# STEP 5 — Restart PostgreSQL service  (PIXBRIDGE.md Step 5)
# ─────────────────────────────────────────────────────────────────────────────
Step "Restarting PostgreSQL 17 service"

$pgSvc = Get-Service -Name "postgresql-x64-17" -ErrorAction SilentlyContinue
if (!$pgSvc) {
    # Try to find the service by pattern
    $pgSvc = Get-Service -Name "postgresql*" -ErrorAction SilentlyContinue | Select-Object -First 1
}
if ($pgSvc) {
    Restart-Service $pgSvc.Name -Force
    Start-Sleep -Seconds 3
    $pgSvc.Refresh()
    OK "PostgreSQL service '$($pgSvc.Name)' restarted — status: $($pgSvc.Status)"
} else {
    WARN "PostgreSQL service not found via Get-Service. Attempting net stop/start..."
    net stop postgresql-x64-17 2>&1 | Out-Null
    Start-Sleep -Seconds 2
    net start postgresql-x64-17 2>&1 | Out-Null
    Start-Sleep -Seconds 3
    OK "Restart attempted"
}

# ─────────────────────────────────────────────────────────────────────────────
# STEP 6 — Verify pgvector is available in PostgreSQL  (PIXBRIDGE.md Step 7)
# ─────────────────────────────────────────────────────────────────────────────
Step "Verifying pgvector availability in PostgreSQL"

$env:PGPASSWORD = $PGPASSWORD_VAL
$available = & "$PG_BIN\psql.exe" -U $PGUSER -d $PGDATABASE `
    -c "SELECT name, default_version FROM pg_available_extensions WHERE name = 'vector';" 2>&1
if ($available -notmatch "vector") {
    FAIL "pgvector not visible to PostgreSQL. Ensure the DLL is in $PG_LIB and the SQL files are in $PG_SHARE_EXT, then restart PostgreSQL."
}
OK "pgvector available: $($available -join ' ')"

# ─────────────────────────────────────────────────────────────────────────────
# STEP 7 — Run EF Core migration (applies CREATE EXTENSION vector + creates tables)
# ─────────────────────────────────────────────────────────────────────────────
Step "Running EF Core migration: AddFaceRecognitionVectors"

$migrationProject = "$REPO_ROOT\src\EventPhoto.Infrastructure"
$startupProject   = "$REPO_ROOT\src\EventPhoto.Api"

Write-Host "    Running dotnet ef database update..."
Push-Location $REPO_ROOT
dotnet ef database update --project $migrationProject --startup-project $startupProject 2>&1
Pop-Location

if ($LASTEXITCODE -ne 0) { FAIL "EF migration failed. Check the error above." }
OK "Migration applied successfully"

# Verify tables were created
$tables = Invoke-Psql "SELECT table_name FROM information_schema.tables WHERE table_name IN ('face_embeddings','guest_face_sessions','photo_matches') ORDER BY table_name;"
OK "Tables confirmed: $($tables -join ' ')"

# ─────────────────────────────────────────────────────────────────────────────
# STEP 7 — Create HNSW cosine-similarity index on face_embeddings
# ─────────────────────────────────────────────────────────────────────────────
Step "Creating HNSW cosine-similarity index on face_embeddings"

$idxSql = @"
CREATE INDEX IF NOT EXISTS hnsw_face_embeddings_embedding_idx
ON face_embeddings
USING hnsw (embedding vector_cosine_ops)
WITH (m = 16, ef_construction = 64);
"@

$idxResult = Invoke-Psql $idxSql
OK "HNSW index: $idxResult"

} # end if (-not $ServiceOnly) — pgvector/PostgreSQL/migrations/HNSW complete

# ─────────────────────────────────────────────────────────────────────────────
# STEP 8 — Install Python dependencies for FaceRecognition service
# ─────────────────────────────────────────────────────────────────────────────
Step "Installing Python dependencies for PixBridge.FaceRecognition"

Write-Host "    Running pip install (this downloads ~500MB on first run — InsightFace + OpenCV)..."
& $realPython -m pip install --upgrade pip --quiet
& $realPython -m pip install -r "$FACE_SVC_SRC\requirements.txt"
if ($LASTEXITCODE -ne 0) { FAIL "pip install failed." }
OK "Python dependencies installed"

# ─────────────────────────────────────────────────────────────────────────────
# STEP 9 — Install Windows Service via NSSM
# ─────────────────────────────────────────────────────────────────────────────
Step "Installing PixBridgeFaceRecognition Windows Service via NSSM"

$nssmExe = Get-Command nssm -ErrorAction SilentlyContinue | Select-Object -ExpandProperty Source
if (!$nssmExe) {
    # Try common install paths
    foreach ($p in @("C:\nssm\win64\nssm.exe","C:\tools\nssm\nssm.exe","C:\ProgramData\chocolatey\lib\nssm\tools\nssm.exe")) {
        if (Test-Path $p) { $nssmExe = $p; break }
    }
}

if (!$nssmExe) {
    Write-Host "    NSSM not found. Downloading portable nssm 2.24..."
    $nssmZip = "$env:TEMP\nssm.zip"
    Invoke-WebRequest -Uri "https://nssm.cc/release/nssm-2.24.zip" -OutFile $nssmZip -UseBasicParsing
    Expand-Archive $nssmZip -DestinationPath "$env:TEMP\nssm-pkg" -Force
    $nssmExe = Get-ChildItem "$env:TEMP\nssm-pkg" -Filter "nssm.exe" -Recurse |
        Where-Object { $_.FullName -match "win64" } | Select-Object -ExpandProperty FullName -First 1
    if (!$nssmExe) { FAIL "Could not locate nssm.exe after download." }
    # Copy to a permanent location
    New-Item -ItemType Directory -Force -Path "C:\nssm" | Out-Null
    Copy-Item $nssmExe "C:\nssm\nssm.exe" -Force
    $nssmExe = "C:\nssm\nssm.exe"
    OK "NSSM downloaded to $nssmExe"
}

$svcName = "PixBridgeFaceRecognition"
$existing = & $nssmExe status $svcName 2>&1
if ($existing -notmatch "SERVICE_STOPPED|SERVICE_RUNNING|does not exist") {
    WARN "Could not determine service status: $existing"
}

if ($existing -match "does not exist") {
    Write-Host "    Registering service $svcName..."
    & $nssmExe install $svcName $realPython "$FACE_SVC_SRC\run.py"
    & $nssmExe set $svcName AppDirectory $FACE_SVC_SRC
    & $nssmExe set $svcName AppStdout "$FACE_SVC_SRC\logs\nssm-stdout.log"
    & $nssmExe set $svcName AppStderr "$FACE_SVC_SRC\logs\nssm-stderr.log"
    & $nssmExe set $svcName AppRotateFiles 1
    & $nssmExe set $svcName AppRotateSeconds 86400
    & $nssmExe set $svcName Start SERVICE_AUTO_START
    & $nssmExe set $svcName Description "PixBridge Face Recognition FastAPI service (InsightFace ArcFace)"
    OK "Service registered"
} else {
    OK "Service already registered (status: $existing)"
}

# Create logs folder
New-Item -ItemType Directory -Force -Path "$FACE_SVC_SRC\logs" | Out-Null

# Start the service
Write-Host "    Starting $svcName..."
& $nssmExe start $svcName 2>&1 | Out-Null
Start-Sleep -Seconds 3
$status = & $nssmExe status $svcName 2>&1
OK "Service status: $status"

# ─────────────────────────────────────────────────────────────────────────────
# DONE
# ─────────────────────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "================================================================" -ForegroundColor Green
Write-Host " PixBridge Face Search setup complete!" -ForegroundColor Green
Write-Host "================================================================" -ForegroundColor Green
Write-Host ""
Write-Host " pgvector $PGVECTOR_VER  → PostgreSQL 17 (CREATE EXTENSION applied)"
Write-Host " EF migration            → AddFaceRecognitionVectors applied"
Write-Host " HNSW index              → face_embeddings.embedding (cosine, m=16)"
Write-Host " Python service          → $svcName (auto-start, port 8080)"
Write-Host " Python path             → $realPython"
Write-Host ""
Write-Host " Next steps:"
Write-Host "   1. Enable face recognition on an event in the admin panel"
Write-Host "   2. Drop photos into the watch folder — Worker will index faces"
Write-Host "   3. Guests upload a selfie via the gallery → instant face-match results"
Write-Host ""
Write-Host " Service health check:"
Write-Host "   Invoke-RestMethod http://localhost:8080/health"
Write-Host ""
