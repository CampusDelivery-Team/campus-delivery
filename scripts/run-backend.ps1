$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "..\backend\src\CampusDelivery.Api"
Set-Location $projectPath

dotnet run
