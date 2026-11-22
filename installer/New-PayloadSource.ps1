param(
    [Parameter(Mandatory)][string]$Payload,
    [Parameter(Mandatory)][string]$Destination
)
$ErrorActionPreference = 'Stop'

function Get-InstallerId {
    param([string]$Prefix, [string]$RelativePath)
    $bytes = [Text.Encoding]::UTF8.GetBytes($RelativePath.ToLowerInvariant())
    $digest = $sha256.ComputeHash($bytes)
    return $Prefix + [BitConverter]::ToString($digest, 0, 16).Replace('-', '')
}

function Write-PayloadDirectory {
    param([IO.DirectoryInfo]$Folder, [string]$DirectoryId)

    foreach ($file in Get-ChildItem -LiteralPath $Folder.FullName -File -Force | Sort-Object Name) {
        $relativePath = $file.FullName.Substring($root.FullName.Length + 1)
        $componentId = Get-InstallerId 'File_' $relativePath
        # A component keeps its identity across releases and staging directories.
        $componentGuid = [guid]::ParseExact((Get-InstallerId '' "Wii2Xbox MSI x64/$relativePath"), 'N')
        $xml.WriteStartElement('Component')
        $xml.WriteAttributeString('Id', $componentId)
        $xml.WriteAttributeString('Guid', $componentGuid.ToString())
        $xml.WriteStartElement('File')
        $xml.WriteAttributeString('Source', $file.FullName)
        $xml.WriteEndElement()
        # Windows Installer requires an HKCU key path for files in a user profile.
        $xml.WriteStartElement('RegistryValue')
        $xml.WriteAttributeString('Root', 'HKCU')
        $xml.WriteAttributeString('Key', 'Software\Wii2Xbox\Installer\Files')
        $xml.WriteAttributeString('Name', $componentId)
        $xml.WriteAttributeString('Type', 'integer')
        $xml.WriteAttributeString('Value', '1')
        $xml.WriteAttributeString('KeyPath', 'yes')
        $xml.WriteEndElement()
        $xml.WriteEndElement()
        $components.Add($componentId)
    }

    foreach ($child in Get-ChildItem -LiteralPath $Folder.FullName -Directory -Force | Sort-Object Name) {
        $childId = Get-InstallerId 'Dir_' $child.FullName.Substring($root.FullName.Length + 1)
        $xml.WriteStartElement('Directory')
        $xml.WriteAttributeString('Id', $childId)
        $xml.WriteAttributeString('Name', $child.Name)
        Write-PayloadDirectory $child $childId
        $xml.WriteEndElement()
    }

    $directories.Add($DirectoryId)
}

$root = Get-Item -LiteralPath $Payload
$components = [Collections.Generic.List[string]]::new()
$directories = [Collections.Generic.List[string]]::new()
$sha256 = [Security.Cryptography.SHA256]::Create()
$settings = [Xml.XmlWriterSettings]::new()
$settings.Indent = $true
$xml = [Xml.XmlWriter]::Create($Destination, $settings)
try {
    $xml.WriteStartElement('Wix', 'http://wixtoolset.org/schemas/v4/wxs')
    $xml.WriteStartElement('Fragment')
    $xml.WriteStartElement('DirectoryRef')
    $xml.WriteAttributeString('Id', 'InstallFolder')
    Write-PayloadDirectory $root 'InstallFolder'
    $xml.WriteEndElement()
    $xml.WriteStartElement('ComponentGroup')
    $xml.WriteAttributeString('Id', 'ApplicationFiles')
    foreach ($component in $components) {
        $xml.WriteStartElement('ComponentRef')
        $xml.WriteAttributeString('Id', $component)
        $xml.WriteEndElement()
    }
    $xml.WriteEndElement()
    $xml.WriteStartElement('ComponentGroup')
    $xml.WriteAttributeString('Id', 'PayloadDirectoryCleanup')
    $xml.WriteAttributeString('Directory', 'InstallFolder')
    $xml.WriteStartElement('Component')
    $xml.WriteAttributeString('Id', 'RemovePayloadDirectories')
    $xml.WriteStartElement('RegistryValue')
    $xml.WriteAttributeString('Root', 'HKCU')
    $xml.WriteAttributeString('Key', 'Software\Wii2Xbox\Installer')
    $xml.WriteAttributeString('Name', 'DirectoryCleanup')
    $xml.WriteAttributeString('Type', 'integer')
    $xml.WriteAttributeString('Value', '1')
    $xml.WriteAttributeString('KeyPath', 'yes')
    $xml.WriteEndElement()
    foreach ($directory in $directories) {
        $xml.WriteStartElement('RemoveFolder')
        $xml.WriteAttributeString('Id', "Remove_$directory")
        $xml.WriteAttributeString('Directory', $directory)
        $xml.WriteAttributeString('On', 'uninstall')
        $xml.WriteEndElement()
    }
    $xml.WriteEndDocument()
} finally {
    $xml.Dispose()
    $sha256.Dispose()
}
