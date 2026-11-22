param(
    [string]$Dotnet = 'dotnet',
    [ValidatePattern('^(0|[1-9][0-9]{0,2})\.(0|[1-9][0-9]{0,2})\.(0|[1-9][0-9]{0,4})$')]
    [string]$Version = '1.0.1'
)
$ErrorActionPreference = 'Stop'

$productVersion = [version]$Version
if ($productVersion.Major -gt 255 -or $productVersion.Minor -gt 255 -or $productVersion.Build -gt 65535) {
    throw 'La versión debe respetar el límite de Windows Installer de 255.255.65535.'
}

Push-Location (Join-Path $PSScriptRoot '..')
try {
    $artifacts = (New-Item -ItemType Directory -Path artifacts -Force).FullName
    $stage = (New-Item -ItemType Directory -Path (Join-Path $artifacts "package-$([guid]::NewGuid())")).FullName
    if ([IO.Path]::GetDirectoryName($stage) -ne $artifacts) {
        throw 'El directorio temporal debe estar dentro de artifacts.'
    }
    try {
        $payload = Join-Path $stage 'win-x64'
        & "$PSScriptRoot/publish.ps1" -Dotnet $Dotnet -OutputDirectory $payload -Version $Version
        $payloadSource = Join-Path $stage 'Payload.wxs'
        & ./installer/New-PayloadSource.ps1 -Payload $payload -Destination $payloadSource
        $installerLicense = Join-Path $stage 'LICENSE.rtf'
        $licenseLines = Get-Content -LiteralPath (Join-Path $payload 'LICENSE') | ForEach-Object {
            $_.Replace('\', '\\').Replace('{', '\{').Replace('}', '\}') + '\par'
        }
        Set-Content -LiteralPath $installerLicense -Encoding Ascii -Value (@('{\rtf1\ansi\deff0{\fonttbl{\f0 Segoe UI;}}\fs18') + $licenseLines + '}')
        & $Dotnet build installer/Wii2Xbox.Installer.wixproj -c Release -o $stage "-p:PayloadSource=$payloadSource" "-p:ProductVersion=$Version" "-p:InstallerLicense=$installerLicense"
        if ($LASTEXITCODE -ne 0) { throw "No se pudo generar el MSI (código $LASTEXITCODE)." }

        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $zip = Join-Path $stage 'Wii2Xbox-win-x64.zip'
        [IO.Compression.ZipFile]::CreateFromDirectory($payload, $zip)

        foreach ($name in @('Wii2Xbox-win-x64.msi', 'Wii2Xbox-win-x64.zip')) {
            Copy-Item -LiteralPath (Join-Path $stage $name) -Destination $artifacts -Force
            Get-Item -LiteralPath (Join-Path $artifacts $name)
        }
    } finally {
        # The staging directory is created under artifacts for this run only.
        Remove-Item -LiteralPath $stage -Recurse -Force
    }
} finally { Pop-Location }
