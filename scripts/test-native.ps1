param(
    [string]$Dotnet = 'dotnet',
    [ValidateRange(0,900)][int]$SustainSeconds = 0,
    [ValidateSet(1,4)][int]$SustainControllers = 4,
    [switch]$DiagnosticLoad
)
$ErrorActionPreference = 'Stop'
if ($DiagnosticLoad -and $SustainSeconds) { throw 'Selecciona el ensayo sostenido o la carga de diagnóstico.' }
Push-Location (Join-Path $PSScriptRoot '..')
$previousEvidence = $env:WII2XBOX_NATIVE_EVIDENCE
$previousDiagnostics = $env:WII2XBOX_NATIVE_DIAGNOSTICS
$previousDuration = $env:WII2XBOX_SUSTAINED_SECONDS
$previousControllers = $env:WII2XBOX_SUSTAINED_CONTROLLERS
$previousDiagnosticLoad = $env:WII2XBOX_DIAGNOSTIC_LOAD
try {
    $env:WII2XBOX_NATIVE_EVIDENCE = Join-Path (Get-Location).Path ("artifacts/native-test-" + [guid]::NewGuid().ToString('N'))
    $nativeBuild = Join-Path $env:WII2XBOX_NATIVE_EVIDENCE 'build'
    $diagnosticsOutput = Join-Path $env:WII2XBOX_NATIVE_EVIDENCE 'diagnostics'
    Write-Host "Informes en $env:WII2XBOX_NATIVE_EVIDENCE"
    & $Dotnet publish tools/Wii2Xbox.Diagnostics/Wii2Xbox.Diagnostics.csproj -c Release -r win-x64 --self-contained true --artifacts-path $nativeBuild -o $diagnosticsOutput
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    $env:WII2XBOX_NATIVE_DIAGNOSTICS = Join-Path $diagnosticsOutput 'Wii2Xbox.Diagnostics.exe'
    $slotCount = if ($SustainSeconds) { $SustainControllers } else { 4 }
    & $env:WII2XBOX_NATIVE_DIAGNOSTICS self-test $slotCount
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    if ($DiagnosticLoad) {
        $env:WII2XBOX_DIAGNOSTIC_LOAD = '1'
        & $Dotnet test tests/Wii2Xbox.Tests/Wii2Xbox.Tests.csproj -c Release --artifacts-path $nativeBuild --filter 'FullyQualifiedName~SnapshotAndJsonExport|FullyQualifiedName~CaptureOverhead'
        $code = $LASTEXITCODE
        foreach ($name in 'diagnostic-load.json','capture-overhead.json') {
            $report = Join-Path $env:WII2XBOX_NATIVE_EVIDENCE $name
            if (Test-Path -LiteralPath $report) { Get-Content -LiteralPath $report -Raw }
        }
        if ($code -ne 0) { exit $code }
        return
    }
    if ($SustainSeconds) {
        $env:WII2XBOX_SUSTAINED_SECONDS = "$SustainSeconds"
        $env:WII2XBOX_SUSTAINED_CONTROLLERS = "$SustainControllers"
        & $Dotnet test tests/Wii2Xbox.Tests/Wii2Xbox.Tests.csproj -c Release --artifacts-path $nativeBuild --filter 'FullyQualifiedName~SustainedWiiTraffic'
        $code = $LASTEXITCODE
        $report = Join-Path $env:WII2XBOX_NATIVE_EVIDENCE 'sustained.json'
        if (Test-Path -LiteralPath $report) {
            (Get-Content -LiteralPath $report -Raw | ConvertFrom-Json).Sessions |
                Select-Object Player,
                    @{n='Seconds';e={$_.Statistics.WindowSeconds}},
                    @{n='Samples';e={$_.Statistics.SampleCount}},
                    @{n='P95Ms';e={$_.Statistics.P95Ms}},
                    @{n='P99Ms';e={$_.Statistics.P99Ms}},
                    @{n='MaxMs';e={$_.Statistics.MaxMs}}
        }
        if ($code -ne 0) { exit $code }
        return
    }
    & $Dotnet test tests/Wii2Xbox.Tests/Wii2Xbox.Tests.csproj -c Release --artifacts-path $nativeBuild --filter 'FullyQualifiedName~SyntheticWiiReportsReachRealXInput'
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
    foreach ($count in 1,4) {
        $results = & ./scripts/compare-xinput.ps1 `
            -AppReport (Join-Path $env:WII2XBOX_NATIVE_EVIDENCE "app-$count.json") `
            -ObserverReport (Join-Path $env:WII2XBOX_NATIVE_EVIDENCE "observer-$count.json")
        $results | Set-Content -LiteralPath (Join-Path $env:WII2XBOX_NATIVE_EVIDENCE "comparison-$count.json") -Encoding UTF8
        Write-Host "$count sesión(es)"
        $results
    }
    Write-Host "Informes en $env:WII2XBOX_NATIVE_EVIDENCE"
} finally {
    $env:WII2XBOX_NATIVE_EVIDENCE = $previousEvidence
    $env:WII2XBOX_NATIVE_DIAGNOSTICS = $previousDiagnostics
    $env:WII2XBOX_SUSTAINED_SECONDS = $previousDuration
    $env:WII2XBOX_SUSTAINED_CONTROLLERS = $previousControllers
    $env:WII2XBOX_DIAGNOSTIC_LOAD = $previousDiagnosticLoad
    Pop-Location
}
