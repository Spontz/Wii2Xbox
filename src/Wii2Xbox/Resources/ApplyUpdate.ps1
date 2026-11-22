$ErrorActionPreference = 'Stop'

function Test-ReparsePoint([string]$path) {
    if (Test-Path -LiteralPath $path) {
        return [bool]([IO.File]::GetAttributes($path) -band [IO.FileAttributes]::ReparsePoint)
    }
    return $false
}

function Resolve-PayloadFile([string]$root, [string]$name) {
    $path = $root
    foreach ($part in ($name -split '/')) {
        if ($part -notmatch '^[A-Za-z0-9_.-]+$' -or $part.EndsWith('.') -or
            $part -match '^(CON|PRN|AUX|NUL|COM[1-9]|LPT[1-9])(\.|$)') {
            throw "Invalid update payload path: $name"
        }
        $path = Join-Path $path $part
        if (Test-ReparsePoint $path) { throw "Reparse point in update payload path: $name" }
    }
    return $path
}

function Read-PayloadManifest([string]$root) {
    $names = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($name in [IO.File]::ReadAllLines((Resolve-PayloadFile $root 'update-files.txt'))) {
        $null = Resolve-PayloadFile $root $name
        if (!$names.Add($name)) { throw "Duplicate update payload entry: $name" }
    }
    if (!$names.Contains('update-files.txt') -or !$names.Contains('Wii2Xbox.exe') -or !$names.Contains('Wii2Xbox.dll')) {
        throw 'Incomplete update payload manifest.'
    }
    return ,$names
}

function Get-AppVersion([string]$root) {
    $file = Join-Path $root 'Wii2Xbox.dll'
    return (([Diagnostics.FileVersionInfo]::GetVersionInfo($file).ProductVersion -split '\+', 2)[0])
}

function Write-PayloadFile([string]$source, [string]$destination) {
    $null = [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination))
    $temporary = $destination + '.update-' + [guid]::NewGuid().ToString('N')
    try {
        [IO.File]::Copy($source, $temporary)
        if ([IO.File]::Exists($destination)) {
            [IO.File]::Replace($temporary, $destination, [NullString]::Value)
        } else {
            [IO.File]::Move($temporary, $destination)
        }
    } finally { [IO.File]::Delete($temporary) }
}

function Apply-Portable([IO.Stream]$package, [string]$directory, [string]$version) {
    if (Test-ReparsePoint $directory) { throw 'The app folder is a reparse point.' }
    Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
    $payload = Join-Path $request.Directory 'payload'
    $backup = Join-Path $request.Directory 'backup'
    $null = [IO.Directory]::CreateDirectory($payload)
    $null = [IO.Directory]::CreateDirectory($backup)
    $archive = [IO.Compression.ZipArchive]::new($package, [IO.Compression.ZipArchiveMode]::Read, $true)
    $entries = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    try {
        $expandedSize = 0L
        foreach ($entry in $archive.Entries) {
            $name = $entry.FullName.Replace('\', '/')
            if ($name.EndsWith('/')) { continue }
            $expandedSize += $entry.Length
            if ($expandedSize -gt 512MB -or $entries.Count -ge 2000 -or !$entries.Add($name)) {
                throw 'The portable package exceeds supported limits or contains duplicate paths.'
            }
            $destination = Resolve-PayloadFile $payload $name
            $null = [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($destination))
            [IO.Compression.ZipFileExtensions]::ExtractToFile($entry, $destination)
        }
    } finally { $archive.Dispose() }
    $previous = Read-PayloadManifest $directory
    $next = Read-PayloadManifest $payload
    if (!$entries.SetEquals($next) -or (Get-AppVersion $payload) -ne $version) {
        throw 'The portable payload does not match its manifest or version.'
    }
    $paths = [Collections.Generic.HashSet[string]]::new($previous, [StringComparer]::OrdinalIgnoreCase)
    $paths.UnionWith($next)
    $changes = [Collections.Generic.List[object]]::new()
    foreach ($name in $paths) {
        $destination = Resolve-PayloadFile $directory $name
        $exists = [IO.File]::Exists($destination)
        if ([IO.Directory]::Exists($destination) -or ($exists -and !$previous.Contains($name))) {
            throw "The update would overwrite a file outside the existing app payload: $name"
        }
        if (!$exists -and !$next.Contains($name)) { continue }
        $saved = Resolve-PayloadFile $backup $name
        if ($exists) {
            $null = [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($saved))
            [IO.File]::Copy($destination, $saved)
        }
        $changes.Add([pscustomobject]@{
            Destination = $destination
            Backup = $saved
            Existed = $exists
            Source = if ($next.Contains($name)) { Resolve-PayloadFile $payload $name } else { $null }
        })
    }

    $applied = 0
    try {
        foreach ($change in $changes) {
            if ($change.Source) { Write-PayloadFile $change.Source $change.Destination }
            else { [IO.File]::Delete($change.Destination) }
            $applied++
        }
        if ((Get-AppVersion $directory) -ne $version) { throw 'Unexpected version after portable update.' }
    } catch {
        for ($index = $applied - 1; $index -ge 0; $index--) {
            $change = $changes[$index]
            try {
                if ($change.Existed) { Write-PayloadFile $change.Backup $change.Destination }
                else { [IO.File]::Delete($change.Destination) }
            } catch { $script:keepBackup = $true }
        }
        throw
    }
}

$request = ConvertFrom-Json $env:WII2XBOX_UPDATE
[Environment]::SetEnvironmentVariable('WII2XBOX_UPDATE', $null)
$root = Join-Path ([Environment]::GetFolderPath('LocalApplicationData')) 'Wii2Xbox\Updates'
$stage = [IO.Path]::GetFullPath($request.Directory)
$expectedName = if ($request.Installer) { 'Wii2Xbox-win-x64.msi' } else { 'Wii2Xbox-win-x64.zip' }
if ((Test-ReparsePoint $root) -or [IO.Path]::GetDirectoryName($stage) -ne $root -or [IO.Path]::GetFileName($stage) -notmatch '^[a-f0-9]{32}$' -or
    $request.AssetPath -ne (Join-Path $stage $expectedName) -or (Test-ReparsePoint $stage) -or (Test-ReparsePoint $request.AssetPath)) {
    throw 'Unexpected update staging directory.'
}

$process = [Diagnostics.Process]::GetProcessById($request.ProcessId)
if ($process.StartTime.ToUniversalTime().Ticks -ne [long]$request.ProcessStarted -or
    $process.MainModule.FileName -ne $request.Executable) { throw 'The waiting app process changed.' }
$application = [IO.Path]::GetDirectoryName($request.Executable)
$mutex = [Threading.Mutex]::new($false, $request.Mutex)
$ready = [Threading.EventWaitHandle]::OpenExisting($request.ReadyEvent)
$start = [Threading.EventWaitHandle]::OpenExisting($request.StartEvent)
$locked = $false
$committed = $false
$script:keepBackup = $false
$result = 'failed'
try {
    try { $locked = $mutex.WaitOne(0) } catch [Threading.AbandonedMutexException] { $locked = $true }
    if (!$locked) { return }
    $null = $ready.Set()
    if (!$start.WaitOne(10000)) { return }
    $committed = $true
    if (!$process.WaitForExit(20000)) { return }
    $package = [IO.File]::Open($request.AssetPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $sha256 = [Security.Cryptography.SHA256]::Create()
        try { $digest = [BitConverter]::ToString($sha256.ComputeHash($package)).Replace('-', '') }
        finally { $sha256.Dispose() }
        if ($digest -ne $request.Sha256) {
            throw 'Update digest changed before installation.'
        }
        $package.Position = 0
        if ([version](Get-AppVersion $application) -ge [version]$request.Version) { throw 'Update is no longer newer.' }
        if ($request.Installer) {
            $msi = Join-Path ([Environment]::SystemDirectory) 'msiexec.exe'
            $arguments = '/i "{0}" /qn /norestart REBOOT=ReallySuppress' -f $request.AssetPath
            $installation = Start-Process -FilePath $msi -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
            if ($installation.ExitCode -in 1641, 3010) { $result = 'restart-required' }
            elseif ($installation.ExitCode -eq 0 -and (Get-AppVersion $application) -eq $request.Version) { $result = 'ok' }
        } else {
            Apply-Portable $package $application $request.Version
            $result = 'ok'
        }
    } finally { $package.Dispose() }
} catch {
    [Diagnostics.Debug]::WriteLine([string]$_)
} finally {
    if ($locked) { $mutex.ReleaseMutex() }
    $mutex.Dispose()
    $start.Dispose()
    $ready.Dispose()
    if ($committed -and $process.HasExited) {
        if (Test-Path -LiteralPath $request.Executable -PathType Leaf) {
            $arguments = @("--update-result=$result")
            if ($request.Background) { $arguments += '--background' }
            Start-Process -FilePath $request.Executable -ArgumentList $arguments -WorkingDirectory $application
        }
        if (!$script:keepBackup) {
            Set-Location -LiteralPath $root
            Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    $process.Dispose()
}
