$ErrorActionPreference = "Stop"

$schemaPath = Resolve-Path (Join-Path $PSScriptRoot "..\database\oracle\campus_runner_oracle_schema.sql")

Write-Host "Open your Oracle client as APPUSER and run:"
Write-Host $schemaPath
Write-Host ""
Write-Host "Then verify with:"
Write-Host "SELECT COUNT(*) FROM users;"
