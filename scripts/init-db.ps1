$ErrorActionPreference = "Stop"

$schemaPath = Resolve-Path (Join-Path $PSScriptRoot "..\database\oracle\campus_runner_oracle_schema.sql")
$baseDataPath = Resolve-Path (Join-Path $PSScriptRoot "..\database\oracle\002_init_base_data.sql")

Write-Host "This script only prints database initialization paths."
Write-Host "Do NOT run the schema script against the shared database unless confirmed by the database maintainer."
Write-Host ""
Write-Host "Execution order for a new or rebuild database:"
Write-Host "1. $schemaPath"
Write-Host "2. $baseDataPath"
Write-Host ""
Write-Host "Expected base data after 002:"
Write-Host "users = 3"
Write-Host "nodes = 3"
Write-Host "service_types = 3"
Write-Host "service_node_rules = 4"
