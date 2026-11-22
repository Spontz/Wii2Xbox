using System.Diagnostics;

namespace Wii2Xbox;

internal sealed class LatencyMetrics
{
    public const int DefaultCapacity = 131_072;
    private readonly Sample[] samples;
    private readonly int mask;
    private long published;

    private struct Sample
    {
        public long Sequence;
        public long Received;
        public long Elapsed;
        public bool XboxA;
    }

    internal LatencyMetrics(int capacity = DefaultCapacity)
    {
        if (capacity < 2 || (capacity & (capacity - 1)) != 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        samples = new Sample[capacity];
        mask = capacity - 1;
    }

    // The session is the only writer. Readers can skip an overwritten sample
    // instead of holding a lock that could delay a later input report.
    public void Record(long received, long submitted, bool xboxA = false)
    {
        long sequence = published + 1;
        ref var sample = ref samples[(int)((sequence - 1) & mask)];
        Interlocked.Exchange(ref sample.Sequence, 0);
        sample.Received = received;
        sample.Elapsed = submitted - received;
        sample.XboxA = xboxA;
        Volatile.Write(ref sample.Sequence, sequence);
        Volatile.Write(ref published, sequence);
    }

    public LatencySnapshot Snapshot()
    {
        long last = Volatile.Read(ref published);
        int size = (int)Math.Min(last, samples.Length);
        var elapsed = new long[size];
        long firstTime = 0;
        long lastTime = 0;
        int count = 0;
        bool lastA = false;
        long previousSequence = 0;
        var transitions = new List<XboxAEdge>();
        for (long sequence = last - size + 1; sequence <= last; sequence++)
        {
            ref var sample = ref samples[(int)((sequence - 1) & mask)];
            if (Volatile.Read(ref sample.Sequence) != sequence) continue;
            long received = sample.Received;
            long duration = sample.Elapsed;
            bool pressed = sample.XboxA;
            if (Interlocked.Read(ref sample.Sequence) != sequence) continue;
            if (count == 0) firstTime = received;
            lastTime = received;
            if (previousSequence == sequence - 1 && count > 0 && pressed != lastA)
                transitions.Add(new XboxAEdge(received, duration + received, pressed));
            lastA = pressed;
            previousSequence = sequence;
            elapsed[count++] = duration;
        }

        if (count == 0)
            return new(0, last, last - size, size, samples.Length, 0, null, null, null, null, null, 0, 0, []);

        Array.Sort(elapsed, 0, count);
        double window = (lastTime - firstTime) / (double)Stopwatch.Frequency;
        double? rate = window > 0 && count == size ? (count - 1) / window : null;
        return new(count, last, last - size, size - count, samples.Length, window, rate,
            Milliseconds(elapsed[(count - 1) / 2]),
            Percentile(elapsed, count, 95),
            Percentile(elapsed, count, 99),
            Milliseconds(elapsed[count - 1]), firstTime, lastTime, transitions.ToArray());
    }

    public static double Milliseconds(long ticks) => ticks * (1000.0 / Stopwatch.Frequency);

    private static double Percentile(long[] sorted, int count, int percentile)
    {
        int index = (int)((count * (long)percentile + 99) / 100) - 1;
        return Milliseconds(sorted[index]);
    }
}

internal sealed record LatencySnapshot(
    int SampleCount, long TotalCaptured, long ExpiredByCapacity, int SkippedDuringSnapshot,
    int Capacity, double WindowSeconds, double? ReportsPerSecond,
    double? P50Ms, double? P95Ms, double? P99Ms, double? MaxMs,
    long FirstReceivedTick, long LastReceivedTick, XboxAEdge[] XboxATransitions);

internal readonly record struct XboxAEdge(long ReceivedTick, long SubmittedTick, bool Pressed);
