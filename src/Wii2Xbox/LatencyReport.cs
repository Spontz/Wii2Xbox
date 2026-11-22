using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Wii2Xbox;

internal static class LatencyReport
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static async Task WriteAsync(string path, ControllerSession session, LatencySnapshot statistics, string conditions)
    {
        var report = new
        {
            SchemaVersion = 2,
            CapturedAtUtc = DateTimeOffset.UtcNow,
            Measurement = "hid-read-to-vigem-submit",
            ClockFrequencyHz = Stopwatch.Frequency,
            ApplicationVersion = typeof(LatencyReport).Assembly.GetName().Version?.ToString(),
            Windows = RuntimeInformation.OSDescription,
            Runtime = RuntimeInformation.FrameworkDescription,
            Processor = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER"),
            Controller = session.Model,
            Player = session.Player,
            CurrentProfile = session.ProfileName,
            CurrentOrientation = session.Horizontal ? "horizontal" : "vertical",
            ProfileChangesInSession = session.ProfileChanges,
            Conditions = conditions.Trim(),
            TruncatedHidReports = session.TruncatedReports,
            UnknownHidReports = session.UnknownReports,
            Statistics = statistics
        };
        await using var file = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);
        await JsonSerializer.SerializeAsync(file, report, JsonOptions).ConfigureAwait(false);
    }
}
