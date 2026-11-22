param(
    [Parameter(Mandatory)][string]$AppReport,
    [Parameter(Mandatory)][string]$ObserverReport
)
$ErrorActionPreference = 'Stop'
$app = Get-Content -LiteralPath $AppReport -Raw | ConvertFrom-Json
$observer = Get-Content -LiteralPath $ObserverReport -Raw | ConvertFrom-Json
if ($app.SchemaVersion -ne 2 -or $observer.SchemaVersion -ne 1 -or
    $app.Measurement -ne 'hid-read-to-vigem-submit' -or $observer.Measurement -ne 'xinput-observer-xbox-a') {
    throw 'Los archivos no corresponden a los formatos de captura esperados.'
}
if ($app.ClockFrequencyHz -le 0 -or $app.ClockFrequencyHz -ne $observer.ClockFrequencyHz) {
    throw 'Los archivos deben proceder del mismo Windows durante la misma sesión.'
}
if ($app.Player -ne $observer.Player -or $app.ProfileChangesInSession -ne 0) {
    throw 'Selecciona la misma ranura Xbox en ambos informes y mantén el perfil durante la sesión.'
}
if ($observer.EndTick -le $observer.StartTick -or $app.Statistics.SkippedDuringSnapshot -ne 0 -or
    $app.Statistics.FirstReceivedTick -gt $observer.StartTick -or $app.Statistics.LastReceivedTick -lt $observer.EndTick) {
    throw 'La traza de Wii2Xbox debe cubrir la captura completa del observador sin omisiones.'
}
if ($observer.InitialPressed) { throw 'El botón Xbox A debe estar suelto al iniciar la captura.' }
$expected = @($app.Statistics.XboxATransitions | Where-Object {
    $_.ReceivedTick -ge $observer.StartTick -and $_.ReceivedTick -le $observer.EndTick
})
$observed = @($observer.Transitions)
if ($expected.Count -lt 2 -or $expected.Count -ne $observed.Count -or $expected[-1].Pressed) {
    throw 'No coinciden las transiciones. Pulsa y suelta Xbox A lentamente y deja los extremos de la captura en reposo.'
}
$factor = 1000.0 / $observer.ClockFrequencyHz
$results = for ($i = 0; $i -lt $expected.Count; $i++) {
    $source = $expected[$i]
    $sample = $observed[$i]
    if ($source.Pressed -ne $sample.Pressed -or $sample.ObservedTick -lt $source.ReceivedTick -or
        $sample.ObservedTick -lt $sample.EarliestTick -or
        ($i + 1 -lt $expected.Count -and $sample.ObservedTick -ge $expected[$i + 1].ReceivedTick)) {
        throw "No se puede emparejar de forma inequívoca la transición $($i + 1)."
    }
    [pscustomobject]@{
        ObservedDelayMs = ($sample.ObservedTick - $source.ReceivedTick) * $factor
        PollUncertaintyMs = ($sample.ObservedTick - $sample.EarliestTick) * $factor
    }
}
$delays = @($results.ObservedDelayMs | Sort-Object)
[pscustomobject]@{
    Measurement = 'hid-read-to-xinput-observed-xbox-a'
    Player = $observer.Player
    Transitions = $delays.Count
    ObservedP50Ms = [math]::Round($delays[[int][math]::Ceiling($delays.Count * 0.50) - 1], 3)
    ObservedP95Ms = [math]::Round($delays[[int][math]::Ceiling($delays.Count * 0.95) - 1], 3)
    ObservedP99Ms = [math]::Round($delays[[int][math]::Ceiling($delays.Count * 0.99) - 1], 3)
    MaxPollUncertaintyMs = [math]::Round(($results.PollUncertaintyMs | Measure-Object -Maximum).Maximum, 3)
    ActualObserverHz = [math]::Round($observer.ActualPollHz, 1)
} | ConvertTo-Json
