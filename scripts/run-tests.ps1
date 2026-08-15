param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$testProject = Join-Path $repositoryRoot "backend/tests/CampusDelivery.Tests/CampusDelivery.Tests.csproj"
$schemaFile = Join-Path $repositoryRoot "database/oracle/campus_runner_oracle_schema.sql"
$controllerDirectory = Join-Path $repositoryRoot "backend/src/CampusDelivery.Api/Controllers"
$serviceDirectory = Join-Path $repositoryRoot "backend/src/CampusDelivery.Api/Services"
$repositoryDirectory = Join-Path $repositoryRoot "backend/src/CampusDelivery.Api/Repositories"
$appSettingsFile = Join-Path $repositoryRoot "backend/src/CampusDelivery.Api/appsettings.json"

Push-Location $repositoryRoot
try {
    Write-Host "[TEST] Running focused business-service tests ($Configuration)..."
    dotnet test $testProject --configuration $Configuration --logger "console;verbosity=minimal"
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet test failed with exit code $LASTEXITCODE."
    }

    $tableCount = (Select-String -Path $schemaFile -Pattern '^CREATE TABLE').Count
    if ($tableCount -lt 12) {
        throw "Schema contains only $tableCount tables; at least 12 are required."
    }
    Write-Host "[PASS] Schema table count: $tableCount (requirement: >= 12)."

    $controllerFiles = Get-ChildItem $controllerDirectory -Filter "*Controller.cs" -File
    $httpPostCount = ($controllerFiles | Select-String -Pattern '\[HttpPost').Count
    $antiForgeryCount = ($controllerFiles | Select-String -Pattern '\[ValidateAntiForgeryToken\]').Count
    if ($httpPostCount -ne $antiForgeryCount) {
        throw "POST/anti-forgery mismatch: $httpPostCount POST actions, $antiForgeryCount anti-forgery attributes."
    }
    Write-Host "[PASS] Anti-forgery coverage: $antiForgeryCount/$httpPostCount POST actions."

    $controllerRepositoryLeaks = @(
        $controllerFiles | Select-String -Pattern 'I[A-Za-z0-9]+Repository'
    )
    if ($controllerRepositoryLeaks.Count -ne 0) {
        throw "A controller directly references a repository interface."
    }

    $serviceFiles = Get-ChildItem $serviceDirectory -Recurse -Filter "*.cs" -File
    $servicePersistenceLeaks = @(
        $serviceFiles | Select-String -Pattern 'Oracle(Command|Connection|DataReader|Parameter)|\.CommandText\s*='
    )
    if ($servicePersistenceLeaks.Count -ne 0) {
        throw "A service directly contains Oracle persistence code."
    }
    Write-Host "[PASS] Layer boundaries: controllers use services; services contain no Oracle commands."

    $forUpdateCount = (Get-ChildItem $repositoryDirectory -Recurse -Filter "*.cs" -File |
        Select-String -Pattern 'FOR UPDATE').Count
    if ($forUpdateCount -lt 1) {
        throw "No repository row-lock statement (FOR UPDATE) was found."
    }
    Write-Host "[PASS] Repository row-lock statements found: $forUpdateCount."

    $configuredConnectionString = Select-String -Path $appSettingsFile -Pattern '"OracleDb"\s*:'
    if ($null -eq $configuredConnectionString) {
        throw "ConnectionStrings:OracleDb is not declared in appsettings.json."
    }
    Write-Host "[PASS] Oracle connection string is externally configurable via ConnectionStrings:OracleDb."
}
finally {
    Pop-Location
}
