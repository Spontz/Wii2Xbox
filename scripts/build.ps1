param([string]$Dotnet = 'dotnet')
$ErrorActionPreference = 'Stop'
Push-Location (Join-Path $PSScriptRoot '..')
try {
    & $Dotnet build src/Wii2Xbox/Wii2Xbox.csproj -c Release
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
} finally { Pop-Location }
