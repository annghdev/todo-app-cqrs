param(
    [int]$CoverageThreshold = 53,
    [int]$ApiHostCoverageThreshold = 52
)

$ErrorActionPreference = "Stop"
$coverageDirectory = "tests/Tests/TestResults/coverage"
$coverageXmlPath = Join-Path $coverageDirectory "coverage.cobertura.xml"

dotnet test "tests/Tests/Tests.csproj" `
  /p:CollectCoverage=true `
  /p:CoverletOutputFormat="cobertura%2cjson" `
  /p:CoverletOutput="./TestResults/coverage/" `
  /p:Threshold=$CoverageThreshold `
  /p:ThresholdType=line `
  /p:ThresholdStat=total

if (-not (Test-Path $coverageXmlPath)) {
    throw "Coverage XML report not found at $coverageXmlPath"
}

[xml]$coverageXml = Get-Content $coverageXmlPath
$apiHostPackage = $coverageXml.coverage.packages.package | Where-Object { $_.name -eq "ApiHost" } | Select-Object -First 1

if ($null -eq $apiHostPackage) {
    throw "ApiHost package coverage was not found in $coverageXmlPath"
}

$apiHostCoveragePercent = [math]::Round(([double]$apiHostPackage.'line-rate') * 100, 2)
Write-Host "ApiHost line coverage: $apiHostCoveragePercent%"

if ($apiHostCoveragePercent -lt $ApiHostCoverageThreshold) {
    throw "ApiHost line coverage $apiHostCoveragePercent% is below threshold $ApiHostCoverageThreshold%"
}
