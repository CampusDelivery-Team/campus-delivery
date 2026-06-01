$ErrorActionPreference = "Stop"

$projectPath = Join-Path $PSScriptRoot "..\frontend"
Set-Location $projectPath

npm.cmd run dev
