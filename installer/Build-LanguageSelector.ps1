param(
    [Parameter(Mandatory)][string]$Destination
)

$ErrorActionPreference = 'Stop'
$vswhere = Join-Path "${env:ProgramFiles(x86)}" 'Microsoft Visual Studio/Installer/vswhere.exe'
if (!(Test-Path -LiteralPath $vswhere)) {
    throw 'Building the bilingual MSI requires Visual Studio Build Tools with the x64 C++ tools and Windows SDK.'
}
$visualStudio = & $vswhere -latest -products '*' -requires Microsoft.VisualStudio.Component.VC.Tools.x86.x64 -property installationPath
if (!$visualStudio) {
    throw 'Visual Studio Build Tools with the x64 C++ tools was not found.'
}

$environment = Join-Path $visualStudio 'VC/Auxiliary/Build/vcvars64.bat'
$source = Join-Path $PSScriptRoot 'SelectSetupLanguage.c'
$output = Join-Path $Destination 'SelectSetupLanguage.dll'
$command = 'call "{0}" >nul && cl.exe /nologo /O2 /W4 /WX /MT /LD "{1}" msi.lib /Fe:"{2}"' -f $environment, $source, $output
Push-Location $Destination
try {
    & $env:ComSpec /d /c $command
    if ($LASTEXITCODE -ne 0 -or !(Test-Path -LiteralPath $output)) {
        throw 'Could not compile the Windows Installer language selector.'
    }
} finally { Pop-Location }
