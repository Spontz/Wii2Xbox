using System.Diagnostics;
using Xunit;

namespace Wii2Xbox.Tests;

public sealed class LatencyTests
{
    private static readonly long Millisecond = Stopwatch.Frequency / 1000;

    [Fact]
    public void EmptyAndSingleSampleDoNotInventCadence()
    {
        var metrics = new LatencyMetrics(4);
        Assert.Null(metrics.Snapshot().P95Ms);
        metrics.Record(10 * Millisecond, 12 * Millisecond);
        var actual = metrics.Snapshot();
        Assert.Equal(1, actual.SampleCount);
        Assert.Equal(2, actual.P95Ms!.Value, 3);
        Assert.Null(actual.ReportsPerSecond);
        Assert.Equal(0, actual.WindowSeconds);
    }

    [Fact]
    public void PercentilesUseNearestRankOverAllSamples()
    {
        var metrics = new LatencyMetrics(128);
        for (int i = 1; i <= 100; i++)
            metrics.Record(i * Millisecond * 10, i * Millisecond * 10 + (101 - i) * Millisecond);

        var actual = metrics.Snapshot();
        Assert.Equal(100, actual.SampleCount);
        Assert.Equal(50, actual.P50Ms!.Value, 3);
        Assert.Equal(95, actual.P95Ms!.Value, 3);
        Assert.Equal(99, actual.P99Ms!.Value, 3);
        Assert.Equal(100, actual.MaxMs!.Value, 3);
        Assert.Equal(100, actual.ReportsPerSecond!.Value, 3);
        Assert.Equal(0.99, actual.WindowSeconds, 3);
    }

    [Fact]
    public void FullBufferKeepsLatestSamplesAndReportsExpiredCount()
    {
        var metrics = new LatencyMetrics(4);
        for (int i = 1; i <= 10; i++)
            metrics.Record(i * Millisecond, 2 * i * Millisecond);

        var actual = metrics.Snapshot();
        Assert.Equal(4, actual.SampleCount);
        Assert.Equal(10, actual.TotalCaptured);
        Assert.Equal(6, actual.ExpiredByCapacity);
        Assert.Equal(0, actual.SkippedDuringSnapshot);
        Assert.Equal(8, actual.P50Ms!.Value, 3);
        Assert.Equal(10, actual.MaxMs!.Value, 3);
    }

    [Fact]
    public void TraceContainsOnlyObservedButtonChangesWithinTheRetainedWindow()
    {
        var metrics = new LatencyMetrics(4);
        metrics.Record(10, 12, true);
        metrics.Record(20, 23, false);
        metrics.Record(30, 31, false);
        metrics.Record(40, 43, true);
        metrics.Record(50, 52, false);

        var actual = metrics.Snapshot();
        Assert.Equal(20, actual.FirstReceivedTick);
        Assert.Equal(50, actual.LastReceivedTick);
        Assert.Equal(new[] { new XboxAEdge(40, 43, true), new XboxAEdge(50, 52, false) }, actual.XboxATransitions);
    }

    [Fact]
    public void RecordingDoesNotAllocatePerReport()
    {
        var metrics = new LatencyMetrics(32);
        metrics.Record(0, 1);
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 1; i <= 100_000; i++)
            metrics.Record(i, i + 1);
        long after = GC.GetAllocatedBytesForCurrentThread();
        Assert.Equal(before, after);
        Assert.Equal(100_001, metrics.Snapshot().TotalCaptured);
    }
}
