namespace Wii2Xbox;

// Called from the window thread. A retry never starts before the previous
// session has neutralized and released its outputs.
internal sealed class SessionRecovery(SessionMode mode)
{
    private static readonly int[] Delays = [500, 1000, 2000, 4000];
    private long? due;
    private int attempts;
    public SessionMode Mode { get; } = mode;
    private bool pending;

    public void Suspend(bool wasRunning)
    {
        pending |= wasRunning;
        due = null;
    }

    public bool HasRetry(long now, bool lostConnection)
    {
        pending |= lostConnection;
        if (!pending) return false;
        if (attempts >= Delays.Length) return false;
        due ??= now + Delays[attempts];
        return true;
    }

    public bool IsDue(long now) => due is long scheduled && now >= scheduled;

    public bool TryTakeRetry(long now)
    {
        if (!IsDue(now)) return false;
        due = null;
        attempts++;
        return true;
    }

    public void SessionStarted() => pending = false;
}

internal static class InputDeadline
{
    public const int LimitMs = 850;

    public static int RemainingMs(TimeSpan elapsed)
    {
        double remaining = LimitMs - elapsed.TotalMilliseconds;
        if (remaining <= 0)
            throw new TimeoutException(UiText.Get("ControllerCommunicationLost"));
        return (int)Math.Ceiling(remaining);
    }
}
