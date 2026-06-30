<#
.SYNOPSIS
    Creates the PixBridge PostgreSQL database and user.
.NOTES
    Requires PostgreSQL to be installed and psql to be available in PATH.
#>

param(
    [string]$PgUser = "postgres",
    [string]$DbName = "pixbridge",
    [string]$DbUser = "pixbridge",
    [string]$DbPassword = "pixbridge123"
)

$ErrorActionPreference = "Stop"

Write-Host "Setting up PixBridge PostgreSQL database..." -ForegroundColor Cyan

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

$sql | psql -v ON_ERROR_STOP=1 -U $PgUser
if ($LASTEXITCODE -ne 0) {
    throw "PostgreSQL setup failed."
}

Write-Host "Database '$DbName' and user '$DbUser' created successfully." -ForegroundColor Green
