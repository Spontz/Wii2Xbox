using System.Diagnostics;
using System.Text.Json;
using Xunit;

namespace Wii2Xbox.Tests;

public sealed class NativePipelineTests
{
    [NativeDiagnosticsFact]
    public async Task SnapshotAndJsonExportAndWindowInteractionPreserveProcessingBudget()
    {
        string folder = Environment.GetEnvironmentVariable("WII2XBOX_NATIVE_EVIDENCE")!;
        Directory.CreateDirectory(folder);
        var rounds = new List<DiagnosticRound>();
        var map = new CoreInputMap(new Profile());
        foreach (bool exporting in new[] { false, true, true, false })
        {
            var sessions = new List<ControllerSession>();
            int exports = 0;
            int interactions = 0;
            int minimized = 0;
            NativeWindowHarness? window = null;
            try
            {
                for (int index = 0; index < 4; index++)
                {
                    var transport = new PulseTransport(traffic: true);
                    sessions.Add(new ControllerSession("Wii simulado", () => transport, static setRumble => new XboxOutput(setRumble),
                        map, collectLatency: true));
                }
                await WaitForSlots(sessions);
                Assert.Equal(4, sessions.Select(session => session.Player).Distinct().Count());
                window = await NativeWindowHarness.Open(sessions);
                var driverCheck = Stopwatch.StartNew();
                while (!await window.DetectedReadyDriver())
                {
                    Assert.True(driverCheck.ElapsedMilliseconds < 5000, "La ventana no confirmó el controlador Xbox.");
                    await Task.Delay(50);
                }
                var elapsed = Stopwatch.StartNew();
                if (exporting)
                {
                    while (elapsed.Elapsed < TimeSpan.FromSeconds(5))
                    {
                        var state = await window.Interact(interactions++);
                        if (state == System.Windows.Forms.FormWindowState.Minimized) minimized++;
                        await Task.WhenAll(sessions.Select(session => Task.Run(async () =>
                        {
                            var stats = session.Metrics!.Snapshot();
                            var path = Path.Combine(folder, $"export-round-{rounds.Count + 1}-xbox-{session.Player}.json");
                            await LatencyReport.WriteAsync(path, session, stats, "Carga de diagnóstico durante la prueba nativa.");
                        })));
                        exports += sessions.Count;
                        await Task.Delay(150);
                    }
                }
                else await Task.Delay(5000);

                Assert.All(sessions, session => Assert.False(session.Completion.IsCompleted, session.Status));
                var results = sessions.Select(session => session.Metrics!.Snapshot()).ToArray();
                Assert.All(results, stats => Assert.True(stats.SampleCount >= 200));
                if (exporting) Assert.True(minimized >= 4, $"Minimizaciones observadas: {minimized}");
                rounds.Add(new DiagnosticRound(exporting, exports, interactions, minimized, results.Min(stats => stats.SampleCount),
                    results.Max(stats => stats.P99Ms!.Value)));
            }
            finally
            {
                try { if (window is not null) await window.Close(); }
                finally
                {
                    foreach (var session in sessions) session.Stop();
                    await Task.WhenAll(sessions.Select(session => session.Completion)).WaitAsync(TimeSpan.FromSeconds(5));
                }
            }
        }
        double baseline = rounds.Where(round => !round.Exporting).Max(round => round.WorstP99Ms);
        double loaded = rounds.Where(round => round.Exporting).Max(round => round.WorstP99Ms);
        var evidence = new { ParallelSessions = 4, WindowSeconds = 5, Rounds = rounds, BaselineP99Ms = baseline,
            ExportP99Ms = loaded, IncreaseMs = loaded - baseline };
        await File.WriteAllTextAsync(Path.Combine(folder, "diagnostic-load.json"),
            JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(loaded - baseline <= 0.5, $"Incremento p99: {loaded - baseline:F3} ms");
    }

    private sealed record DiagnosticRound(bool Exporting, int JsonExports, int WindowInteractions, int MinimizedTransitions,
        int MinSamplesPerSession, double WorstP99Ms);

    [NativeDiagnosticsFact]
    public async Task CaptureOverheadStaysWithinProcessingBudget()
    {
        string folder = Environment.GetEnvironmentVariable("WII2XBOX_NATIVE_EVIDENCE")!;
        Directory.CreateDirectory(folder);
        var rounds = new List<CaptureRound>();
        var map = new CoreInputMap(new Profile());
        foreach (bool captureEnabled in new[] { false, true, true, false })
        {
            var sessions = new List<ControllerSession>();
            var independent = new List<LatencyMetrics>();
            try
            {
                for (int index = 0; index < 4; index++)
                {
                    var transport = new PulseTransport(traffic: true, sampleReadTime: true);
                    var samples = new LatencyMetrics(1024);
                    independent.Add(samples);
                    sessions.Add(new ControllerSession("Wii simulado", () => transport,
                        _ => new InstrumentedXbox(transport, samples), map, collectLatency: captureEnabled));
                }
                await WaitForSlots(sessions);
                Assert.Equal(4, sessions.Select(session => session.Player).Distinct().Count());
                Assert.All(sessions, session => Assert.Equal(captureEnabled, session.Metrics is not null));
                await Task.Delay(3500);
                Assert.All(sessions, session => Assert.False(session.Completion.IsCompleted, session.Status));
                var measurements = independent.Select(metrics => metrics.Snapshot()).ToArray();
                Assert.All(measurements, stats =>
                {
                    Assert.True(stats.SampleCount >= 100);
                    Assert.Equal(0, stats.SkippedDuringSnapshot);
                    Assert.Equal(0, stats.ExpiredByCapacity);
                });
                var builtIn = sessions.Select(session => session.Metrics?.Snapshot().SampleCount ?? 0).ToArray();
                if (captureEnabled) Assert.All(builtIn, count => Assert.InRange(count, 100, int.MaxValue));
                rounds.Add(new CaptureRound(captureEnabled, measurements.Min(stats => stats.SampleCount),
                    builtIn.Min(), measurements.Max(stats => stats.P99Ms!.Value)));
            }
            finally
            {
                foreach (var session in sessions) session.Stop();
                await Task.WhenAll(sessions.Select(session => session.Completion)).WaitAsync(TimeSpan.FromSeconds(5));
            }
        }

        double offMin = rounds.Where(round => !round.CaptureEnabled).Min(round => round.WorstP99Ms);
        double onMax = rounds.Where(round => round.CaptureEnabled).Max(round => round.WorstP99Ms);
        var evidence = new
        {
            ParallelSessions = 4, WindowSeconds = 3.5, Rounds = rounds, BestOffP99Ms = offMin,
            WorstOnP99Ms = onMax, ConservativeIncreaseMs = onMax - offMin
        };
        await File.WriteAllTextAsync(Path.Combine(folder, "capture-overhead.json"),
            JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
        Assert.True(onMax - offMin <= 0.5, $"Incremento p99: {onMax - offMin:F3} ms");
    }

    private sealed record CaptureRound(bool CaptureEnabled, int MinIndependentSamples, int MinBuiltInSamples,
        double WorstP99Ms);

    private sealed class InstrumentedXbox(PulseTransport transport, LatencyMetrics samples) : IXboxOutput
    {
        private readonly XboxOutput output = new();
        private bool first = true;
        public int? Player => output.Player;
        public void Submit(XboxInput state)
        {
            output.Submit(state);
            if (!first) samples.Record(transport.LastReadTick, Stopwatch.GetTimestamp());
            first = false;
        }
        public void Dispose() => output.Dispose();
    }

    [NativeSustainedFact]
    public async Task SustainedWiiTrafficReachesViGEm()
    {
        int count = int.Parse(Environment.GetEnvironmentVariable("WII2XBOX_SUSTAINED_CONTROLLERS")!);
        int seconds = int.Parse(Environment.GetEnvironmentVariable("WII2XBOX_SUSTAINED_SECONDS")!);
        string evidence = Environment.GetEnvironmentVariable("WII2XBOX_NATIVE_EVIDENCE")!;
        var sessions = new List<ControllerSession>();
        try
        {
            var map = new CoreInputMap(new Profile());
            for (int index = 0; index < count; index++)
            {
                var transport = new PulseTransport(traffic: true);
                sessions.Add(new ControllerSession("Wii simulado", () => transport, static setRumble => new XboxOutput(setRumble),
                    map, collectLatency: true));
            }
            await WaitForSlots(sessions);
            Assert.Equal(count, sessions.Select(session => session.Player).Distinct().Count());
            await Task.Delay(TimeSpan.FromMilliseconds(seconds * 1000 + 500));
            var rows = sessions.Select(session =>
                new { session.Player, session.Status, Statistics = session.Metrics!.Snapshot() }).ToArray();
            Directory.CreateDirectory(evidence);
            var result = new
            {
                Measurement = "simulated-wii-input-to-real-vigem-submit",
                DurationRequestedSeconds = seconds,
                ParallelSessions = count,
                Windows = Environment.OSVersion.VersionString,
                Sessions = rows
            };
            await File.WriteAllTextAsync(Path.Combine(evidence, "sustained.json"),
                JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            Assert.All(sessions, session => Assert.False(session.Completion.IsCompleted, session.Status));
            Assert.All(rows, row =>
            {
                Assert.True(row.Statistics.WindowSeconds >= seconds, row.Status);
                Assert.True(row.Statistics.SampleCount >= (int)Math.Ceiling(seconds * (10_000.0 / 600)));
                Assert.Equal(0, row.Statistics.ExpiredByCapacity);
                Assert.Equal(0, row.Statistics.SkippedDuringSnapshot);
                Assert.InRange(row.Statistics.P95Ms!.Value, 0, 1);
                Assert.InRange(row.Statistics.P99Ms!.Value, 0, 2);
            });
        }
        finally
        {
            foreach (var session in sessions) session.Stop();
            await Task.WhenAll(sessions.Select(session => session.Completion)).WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    [NativeTheory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task SyntheticWiiReportsReachRealXInput(int count)
    {
        string evidence = Environment.GetEnvironmentVariable("WII2XBOX_NATIVE_EVIDENCE")!;
        string diagnosticsPath = Environment.GetEnvironmentVariable("WII2XBOX_NATIVE_DIAGNOSTICS")!;
        Directory.CreateDirectory(evidence);
        string observerPath = Path.Combine(evidence, $"observer-{count}.json");
        var sessions = new List<ControllerSession>();
        var transports = new List<PulseTransport>();
        try
        {
            var map = new CoreInputMap(new Profile());
            for (int index = 0; index < count; index++)
            {
                var transport = new PulseTransport(traffic: index > 0);
                transports.Add(transport);
                sessions.Add(new ControllerSession("Wii simulado", () => transport, static setRumble => new XboxOutput(setRumble),
                    map, collectLatency: true));
            }

            await WaitForSlots(sessions);
            Assert.Equal(count, sessions.Select(session => session.Player).Distinct().Count());
            await Task.Delay(150);
            var start = new ProcessStartInfo(diagnosticsPath) { UseShellExecute = false, CreateNoWindow = true };
            start.ArgumentList.Add("capture");
            start.ArgumentList.Add(sessions[0].Player!.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            start.ArgumentList.Add("6");
            start.ArgumentList.Add(observerPath);
            using var process = Process.Start(start) ?? throw new InvalidOperationException("No se pudo iniciar el observador XInput.");
            try
            {
                await Task.Delay(900);
                for (int edge = 0; edge < 40; edge++)
                {
                    transports[0].SetPressed(edge % 2 == 0);
                    await Task.Delay(60);
                }
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(9));
                await process.WaitForExitAsync(timeout.Token);
                Assert.Equal(0, process.ExitCode);
            }
            finally
            {
                if (!process.HasExited) process.Kill();
            }
            // Keep writing through the end of the independent observation window.
            await Task.Delay(100);
            var snapshots = sessions.Select(session =>
                new { session.Player, session.Status, Statistics = session.Metrics!.Snapshot() }).ToArray();
            Assert.All(snapshots, result => Assert.True(result.Statistics.SampleCount > 100, result.Status));
            Assert.Equal(40, snapshots[0].Statistics.XboxATransitions.Length);
            var report = new
            {
                SchemaVersion = 2,
                Measurement = "hid-read-to-vigem-submit",
                ClockFrequencyHz = Stopwatch.Frequency,
                Player = snapshots[0].Player,
                ProfileChangesInSession = 0,
                Conditions = "Entrada Wii simulada. ViGEmBus y observador XInput nativos.",
                ParallelSessions = count,
                Windows = Environment.OSVersion.VersionString,
                Statistics = snapshots[0].Statistics,
                Sessions = snapshots
            };
            await File.WriteAllTextAsync(Path.Combine(evidence, $"app-{count}.json"),
                JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
        }
        finally
        {
            foreach (var session in sessions) session.Stop();
            await Task.WhenAll(sessions.Select(session => session.Completion)).WaitAsync(TimeSpan.FromSeconds(5));
        }
    }

    private static async Task WaitForSlots(List<ControllerSession> sessions)
    {
        var wait = Stopwatch.StartNew();
        while (sessions.Any(session => session.Player is null))
        {
            if (sessions.Any(session => session.Completion.IsCompleted) || wait.ElapsedMilliseconds > 5000)
                throw new InvalidOperationException(string.Join(" | ", sessions.Select(session => session.Status)));
            await Task.Delay(25);
        }
    }

    private sealed class PulseTransport(bool traffic, bool sampleReadTime = false) : IControllerTransport
    {
        private int pressed;
        private int frames;
        private bool closed;
        public int InputLength => 22;
        public int ReadTimeout { get; set; }
        public long LastReadTick { get; private set; }
        public void SetPressed(bool value) => Volatile.Write(ref pressed, value ? 1 : 0);
        public int Read(byte[] buffer)
        {
            Thread.Sleep(5);
            ObjectDisposedException.ThrowIf(Volatile.Read(ref closed), this);
            buffer[0] = 0x30;
            buffer[2] = (byte)(traffic ? (++frames / 8) % 2 : Volatile.Read(ref pressed));
            if (sampleReadTime) LastReadTick = Stopwatch.GetTimestamp();
            return 3;
        }
        public void Send(WiiCommand command, bool infrared) { }
        public void Send(InfraredCommand command) { }
        public void Dispose() => Volatile.Write(ref closed, true);
    }
}

internal sealed class NativeDiagnosticsFactAttribute : FactAttribute
{
    public NativeDiagnosticsFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("WII2XBOX_DIAGNOSTIC_LOAD") != "1" ||
            Environment.GetEnvironmentVariable("WII2XBOX_NATIVE_EVIDENCE") is null)
            Skip = "Requiere ViGEmBus activo. Ejecuta scripts/test-native.ps1 -DiagnosticLoad.";
    }
}

internal sealed class NativeTheoryAttribute : TheoryAttribute
{
    public NativeTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable("WII2XBOX_NATIVE_EVIDENCE") is null ||
            Environment.GetEnvironmentVariable("WII2XBOX_NATIVE_DIAGNOSTICS") is null)
            Skip = "Requiere ViGEmBus activo y el observador publicado. Ejecuta scripts/test-native.ps1.";
    }
}

internal sealed class NativeSustainedFactAttribute : FactAttribute
{
    public NativeSustainedFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("WII2XBOX_NATIVE_EVIDENCE") is null ||
            Environment.GetEnvironmentVariable("WII2XBOX_SUSTAINED_SECONDS") is null)
            Skip = "Requiere ViGEmBus activo. Ejecuta scripts/test-native.ps1 -SustainSeconds 600.";
    }
}
