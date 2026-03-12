# Backend test commands

## Run all backend tests

```powershell
dotnet test tests/Tests/Tests.csproj
```

## Run with coverage report (Cobertura + JSON)

```powershell
dotnet test tests/Tests/Tests.csproj `
  /p:CollectCoverage=true `
  /p:CoverletOutputFormat=\"cobertura%2cjson\" `
  /p:CoverletOutput=\"./TestResults/coverage/\"
```

## Suggested CI quality gate

- Test job fails on any failed test.
- Coverage gate starts at **50% line coverage** for `src/ApiHost` and increases over time.
- Use this command in CI:

```powershell
dotnet test tests/Tests/Tests.csproj `
  /p:CollectCoverage=true `
  /p:CoverletOutputFormat=cobertura `
  /p:CoverletOutput=./TestResults/coverage/ `
  /p:Threshold=50 `
  /p:ThresholdType=line `
  /p:ThresholdStat=total
```

## Notes

- Integration tests require Docker because PostgreSQL is provisioned with Testcontainers.
- The suite is configured to use a shared PostgreSQL container and truncates all public tables between tests.
