# One-command local startup:
# 1) Start Prometheus + Loki + Grafana from monitoring/docker-compose.yml
# 2) Run the Aspire AppHost (which starts API + PostgreSQL)
#
# Usage:
#   powershell -ExecutionPolicy Bypass -File .\run-all.ps1
#
# Optional:
#   powershell -ExecutionPolicy Bypass -File .\run-all.ps1 -NoBuild

[CmdletBinding()]
param(
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

Push-Location $PSScriptRoot
try {
    Write-Host "Starting monitoring stack (Prometheus, Loki, Grafana)..." -ForegroundColor Cyan
    docker compose -f ".\monitoring\docker-compose.yml" up -d

    Write-Host "Starting application stack via Aspire AppHost..." -ForegroundColor Cyan
    Write-Host "Press Ctrl+C to stop AppHost. Monitoring containers keep running." -ForegroundColor Yellow
    Write-Host "Stop monitoring later: docker compose -f .\monitoring\docker-compose.yml down" -ForegroundColor Yellow

    $args = @("run", "--project", ".\src\Aspire.AppHost\Aspire.AppHost.csproj")
    if ($NoBuild) {
        $args += "--no-build"
    }

    & dotnet @args
}
finally {
    Pop-Location
}
