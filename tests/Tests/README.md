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
- Coverage gate is stepped up to **53% total line coverage**.
- Additional module gate: **ApiHost >= 52% line coverage**.
- Use this command in CI:

```powershell
powershell -ExecutionPolicy Bypass -File tests/Tests/run-backend-tests.ps1
```

## Notes

- Integration tests require Docker because PostgreSQL is provisioned with Testcontainers.
- The suite is configured to use a shared PostgreSQL container and truncates all public tables between tests.
