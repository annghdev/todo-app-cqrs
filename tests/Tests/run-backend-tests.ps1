param(
    [int]$CoverageThreshold = 50
)

$ErrorActionPreference = "Stop"

dotnet test "tests/Tests/Tests.csproj" `
  /p:CollectCoverage=true `
  /p:CoverletOutputFormat="cobertura%2cjson" `
  /p:CoverletOutput="./TestResults/coverage/" `
  /p:Threshold=$CoverageThreshold `
  /p:ThresholdType=line `
  /p:ThresholdStat=total
