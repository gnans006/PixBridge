<#
.SYNOPSIS
    Builds PixBridge for production release — React frontend + .NET API + Worker.
.DESCRIPTION
    1. Builds and bundles the React frontend (output goes to EventPhoto.Api/wwwroot)
    2. Publishes EventPhoto.Api as a self-contained Windows x64 executable
    3. Publishes EventPhoto.Worker as a self-contained Windows x64 executable
    4. Outputs to /publish folder ready for Inno Setup packaging
#>

param(
    [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path $PSScriptRoot -Parent
$PublishRoot = Join-Path $Root "publish"

Write-Host "===== PixBridge Release Build v$Version =====" -ForegroundColor Cyan

# Step 1: Build React frontend
Write-Host "`n[1/4] Building React frontend..." -ForegroundColor Yellow
Set-Location (Join-Path $Root "src\EventPhoto.React")
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
    -p:Version=$Version `
    -o $WorkerPublish

if ($LASTEXITCODE -ne 0) { throw "Worker publish failed." }

# Step 4: Copy shared config files
Write-Host "`n[4/4] Copying deployment assets..." -ForegroundColor Yellow
$SetupDir = Join-Path $PublishRoot "setup"
New-Item -ItemType Directory -Force -Path $SetupDir | Out-Null
Copy-Item (Join-Path $Root "scripts\setup-postgresql.ps1") $SetupDir -Force
Copy-Item (Join-Path $Root "scripts\install-service.ps1") $SetupDir -Force
Copy-Item (Join-Path $Root "scripts\uninstall-service.ps1") $SetupDir -Force
Copy-Item (Join-Path $Root "scripts\installer.iss") $SetupDir -Force
Copy-Item (Join-Path $Root "docs\README.md") $PublishRoot -Force
Copy-Item (Join-Path $Root "docs\deployment-guide.md") $PublishRoot -Force

Write-Host "`n===== Build complete! Output: $PublishRoot =====" -ForegroundColor Green
Write-Host "API:    $ApiPublish"
Write-Host "Worker: $WorkerPublish"
