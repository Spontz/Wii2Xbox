param(
    [string]$Dotnet = 'dotnet',
    [string]$OutputDirectory = 'artifacts/win-x64',
    [string]$Version = '1.2.4'
)
$ErrorActionPreference = 'Stop'

function Copy-PackageNotice {
    param($Assets, [string]$Id, [string]$Version, [string]$File, [string]$Destination)
    foreach ($folder in $Assets.packageFolders.PSObject.Properties.Name) {
        $candidate = Join-Path $folder "$($Id.ToLowerInvariant())/$Version/$File"
        if (Test-Path -LiteralPath $candidate -PathType Leaf) {
            Copy-Item -LiteralPath $candidate -Destination $Destination
            return
        }
    }
    throw "No se encuentra el aviso $File del paquete $Id $Version."
}

Push-Location (Join-Path $PSScriptRoot '..')
try {
    $dependencies = New-Item -ItemType Directory -Path artifacts/dependencies -Force
    $installer = Join-Path $dependencies.FullName 'ViGEmBus_1.22.0_x64_x86_arm64.exe'
    $installerHash = '89220A7865076B342892F98865F3499FB7C4CFD673159E89D352C360FD014C6A'
    if (!(Test-Path -LiteralPath $installer -PathType Leaf) -or (Get-FileHash -LiteralPath $installer).Hash -ne $installerHash) {
        Invoke-WebRequest 'https://github.com/nefarius/ViGEmBus/releases/download/v1.22.0/ViGEmBus_1.22.0_x64_x86_arm64.exe' -OutFile $installer
    }
    if ((Get-FileHash -LiteralPath $installer).Hash -ne $installerHash -or
        (Get-AuthenticodeSignature -LiteralPath $installer).Status -ne 'Valid') {
        throw 'No se ha podido verificar el instalador oficial de ViGEmBus.'
    }
    & $Dotnet publish src/Wii2Xbox/Wii2Xbox.csproj -c Release -r win-x64 --self-contained true -o $OutputDirectory "-p:Version=$Version" "-p:ViGEmBusInstaller=$installer"
    if ($LASTEXITCODE -ne 0) { throw "No se pudo publicar Wii2Xbox (código $LASTEXITCODE)." }
    Copy-Item README.md -Destination "$OutputDirectory/LEEME.md"
    Copy-Item -LiteralPath LICENSE, NOTICE -Destination $OutputDirectory
    New-Item -ItemType Directory -Path "$OutputDirectory/profiles" -Force | Out-Null
    Copy-Item -Path profiles/*.json -Destination "$OutputDirectory/profiles"
    # Remove development tools left by builds that packaged them in this directory.
    Get-ChildItem -LiteralPath $OutputDirectory -File |
        Where-Object Name -Match '^Wii2Xbox\.Diagnostics\.|^compare-xinput\.ps1$' |
        ForEach-Object { Remove-Item -LiteralPath $_.FullName }
    New-Item -ItemType Directory -Path "$OutputDirectory/docs" -Force | Out-Null
    Copy-Item docs/troubleshooting.md -Destination "$OutputDirectory/docs"
    Copy-Item docs/troubleshooting -Destination "$OutputDirectory/docs" -Recurse -Force
    Copy-Item docs/installation.md -Destination "$OutputDirectory/docs"
    Copy-Item docs/compatibility.md -Destination "$OutputDirectory/docs"
    Copy-Item docs/hardware-validation.md -Destination "$OutputDirectory/docs"

    $assets = Get-Content src/Wii2Xbox/obj/project.assets.json -Raw | ConvertFrom-Json
    $runtime = $assets.project.frameworks.PSObject.Properties.Value.downloadDependencies
    $netCore = ($runtime | Where-Object name -eq 'Microsoft.NETCore.App.Runtime.win-x64').version.TrimStart('[').Split(',')[0]
    $desktop = ($runtime | Where-Object name -eq 'Microsoft.WindowsDesktop.App.Runtime.win-x64').version.TrimStart('[').Split(',')[0]
    $hidSharp = ($assets.libraries.PSObject.Properties.Name | Where-Object { $_ -like 'HidSharp/*' }).Split('/')[1]
    New-Item -ItemType Directory -Path "$OutputDirectory/licenses" -Force | Out-Null
    Copy-Item licenses/*.txt -Destination "$OutputDirectory/licenses"
    Copy-PackageNotice $assets HidSharp $hidSharp 'LICENSE.txt' "$OutputDirectory/licenses/HidSharp.txt"
    Copy-PackageNotice $assets Microsoft.NETCore.App.Runtime.win-x64 $netCore 'LICENSE.TXT' "$OutputDirectory/licenses/dotnet-runtime.txt"
    Copy-PackageNotice $assets Microsoft.NETCore.App.Runtime.win-x64 $netCore 'THIRD-PARTY-NOTICES.TXT' "$OutputDirectory/licenses/dotnet-notices.txt"
    Copy-PackageNotice $assets Microsoft.WindowsDesktop.App.Runtime.win-x64 $desktop 'LICENSE' "$OutputDirectory/licenses/dotnet-desktop.txt"
    $published = (Resolve-Path -LiteralPath $OutputDirectory).ProviderPath
    $manifestName = 'update-files.txt'
    $paths = Get-ChildItem -LiteralPath $published -Recurse -File |
        ForEach-Object { $_.FullName.Substring($published.Length + 1).Replace('\', '/') } |
        Where-Object { $_ -ne $manifestName }
    @($paths + $manifestName | Sort-Object) | Set-Content -LiteralPath (Join-Path $published $manifestName) -Encoding UTF8
} finally { Pop-Location }
