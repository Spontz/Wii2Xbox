param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
Push-Location (Join-Path $PSScriptRoot '..')
try {
    & $Dotnet test tests/Wii2Xbox.Tests/Wii2Xbox.Tests.csproj -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} finally { Pop-Location }
