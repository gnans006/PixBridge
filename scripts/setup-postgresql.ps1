<#
.SYNOPSIS
    Creates the PixBridge PostgreSQL database and user.
.NOTES
    Requires PostgreSQL to be installed and psql to be available in PATH.
    Writes a result log to $LogFile so the installer can detect failure.
#>

param(
    [string]$PgUser     = "postgres",
    [string]$PgPassword = "",          # postgres superuser password (leave blank for trust/peer auth)
    [string]$DbName     = "pixbridge",
    [string]$DbUser     = "pixbridge",
    [string]$DbPassword = "pixbridge123",
    [string]$LogFile    = "$env:TEMP\pixbridge-setup-db.log"
)

$ErrorActionPreference = "Stop"

function Write-Log {
    param([string]$Message, [string]$Level = "INFO")
    $line = "[$(Get-Date -Format 'HH:mm:ss')] [$Level] $Message"
    $line | Tee-Object -FilePath $LogFile -Append | Write-Host -ForegroundColor $(if ($Level -eq "ERROR") { "Red" } elseif ($Level -eq "WARN") { "Yellow" } else { "Cyan" })
}

# Clear previous log
"" | Set-Content $LogFile

Write-Log "=== PixBridge PostgreSQL Setup ==="
Write-Log "Log file: $LogFile"

# ── Pre-check 1: psql available ────────────────────────────────────────────
$psqlCmd = Get-Command psql -ErrorAction SilentlyContinue

if (-not $psqlCmd) {
    # PostgreSQL is installed but its bin folder isn't in PATH — search common locations
    Write-Log "psql not found in PATH. Searching common PostgreSQL install locations..." "WARN"

    $searchRoots = @(
        "$env:ProgramFiles\PostgreSQL",
        "${env:ProgramFiles(x86)}\PostgreSQL",
        "C:\PostgreSQL",
        "D:\PostgreSQL",
        "E:\PostgreSQL"
    )
    $psqlPath = $null
    foreach ($root in $searchRoots) {
        if (Test-Path $root) {
            # Find the highest version folder (e.g. 15, 16, 17)
            $versionDirs = Get-ChildItem $root -Directory -ErrorAction SilentlyContinue |
                Where-Object { $_.Name -match '^\d+' } |
                Sort-Object { [int]($_.Name -replace '[^\d].*','') } -Descending
            foreach ($vd in $versionDirs) {
                $candidate = Join-Path $vd.FullName "bin\psql.exe"
                if (Test-Path $candidate) {
                    $psqlPath = $candidate
                    break
                }
            }
        }
        if ($psqlPath) { break }
    }

    if ($psqlPath) {
        Write-Log "Found psql at: $psqlPath — adding to session PATH." "WARN"
        $binDir = Split-Path $psqlPath -Parent
        $env:PATH = "$binDir;$env:PATH"
        $psqlCmd = Get-Command psql -ErrorAction SilentlyContinue
    }
}

if (-not $psqlCmd) {
    Write-Log "psql not found. Install PostgreSQL 15+ from https://www.postgresql.org/download/windows/" "ERROR"
    Write-Log "After installing, either add the PostgreSQL bin folder to PATH or rerun this script." "ERROR"
    exit 1
}
Write-Log "psql found: $($psqlCmd.Source)"

# ── Pre-check 2: PostgreSQL service running ────────────────────────────────
$pgService = Get-Service | Where-Object { $_.Name -like "postgresql*" -or $_.DisplayName -like "postgresql*" } | Select-Object -First 1
if ($pgService) {
    if ($pgService.Status -ne "Running") {
        Write-Log "PostgreSQL service '$($pgService.Name)' is not running. Starting it..." "WARN"
        try {
            Start-Service -Name $pgService.Name
            Start-Sleep -Seconds 3
            Write-Log "PostgreSQL service started."
        } catch {
            Write-Log "Failed to start PostgreSQL service: $_" "ERROR"
            Write-Log "Start it manually: Start-Service '$($pgService.Name)'" "ERROR"
            exit 1
        }
    } else {
        Write-Log "PostgreSQL service '$($pgService.Name)' is running."
    }
} else {
    Write-Log "No postgresql Windows service found. Assuming external/manual PostgreSQL." "WARN"
}

# ── Pre-check 3: Can we connect? ───────────────────────────────────────────
if ($PgPassword) { $env:PGPASSWORD = $PgPassword }

$testResult = & psql -U $PgUser -c "SELECT 1;" postgres 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Log "Cannot connect to PostgreSQL as '$PgUser'." "ERROR"
    Write-Log "Details: $testResult" "ERROR"
    Write-Log "Check that: (1) PostgreSQL is running, (2) the postgres password is correct." "ERROR"
    Write-Log "If password is needed, rerun with: -PgPassword 'yourpassword'" "ERROR"
    exit 1
}
Write-Log "PostgreSQL connection successful."

# ── Create user + database ─────────────────────────────────────────────────
Write-Log "Creating database user '$DbUser' and database '$DbName'..."

$sql = @"
DO `$`$
BEGIN
    IF NOT EXISTS (SELECT FROM pg_catalog.pg_roles WHERE rolname = '$DbUser') THEN
        EXECUTE format('CREATE USER %I WITH PASSWORD %L', '$DbUser', '$DbPassword');
    END IF;
END
`$`$;

SELECT format('CREATE DATABASE %I OWNER %I', '$DbName', '$DbUser')
WHERE NOT EXISTS (SELECT FROM pg_database WHERE datname = '$DbName')\gexec

GRANT ALL PRIVILEGES ON DATABASE "$DbName" TO "$DbUser";
"@

$output = $sql | psql -v ON_ERROR_STOP=1 -U $PgUser 2>&1
Write-Log "psql output: $output"

if ($LASTEXITCODE -ne 0) {
    Write-Log "PostgreSQL setup SQL failed. Exit code: $LASTEXITCODE" "ERROR"
    Write-Log "Output: $output" "ERROR"
    exit 1
}

Write-Log "Database '$DbName' and user '$DbUser' ready."
Write-Log "=== PostgreSQL setup completed successfully ==="
