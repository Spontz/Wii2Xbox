using System.Numerics;

namespace Wii2Xbox;

internal sealed class WiiButtonState
{
    public const int CIndex = 16;
    public const int ZIndex = 17;
    public const int StickIndex = 18;
    public const int BitCount = 19;
    private const int StickDeflected = 1 << StickIndex;
    private const int StickPreviewDeadzone = 12;
    private const int NunchukPresent = 1 << BitCount;
    private const int ButtonMask = StickDeflected - 1;
    private const int StickXShift = 32;
    private const int StickYShift = 48;
    private readonly long[] lastPress = new long[StickIndex];
    private long held;

    public void Publish(WiiInput input, long timestamp, StickCalibration? calibration = null)
    {
        long next = (int)input.Buttons;
        if (input.HasNunchuk)
        {
            next |= NunchukPresent;
            if (input.C) next |= 1 << CIndex;
            if (input.Z) next |= 1 << ZIndex;
            var axes = calibration ?? StickCalibration.Default;
            int x = input.StickX - axes.X.Center;
            int y = input.StickY - axes.Y.Center;
            if (Math.Abs(x) > StickPreviewDeadzone || Math.Abs(y) > StickPreviewDeadzone)
                next |= StickDeflected | ((long)(ushort)x << StickXShift) | ((long)(ushort)y << StickYShift);
        }
        if (held == next) return;

        if (!input.HasNunchuk && (held & NunchukPresent) != 0)
        {
            Volatile.Write(ref lastPress[CIndex], 0);
            Volatile.Write(ref lastPress[ZIndex], 0);
        }
        uint rising = (uint)(next & ~held & ButtonMask);
        while (rising != 0)
        {
            Volatile.Write(ref lastPress[BitOperations.TrailingZeroCount(rising)], timestamp);
            rising &= rising - 1;
        }
        Volatile.Write(ref held, next);
    }

    public bool Accumulate(long cutoff, Span<int> counts, out bool hasNunchuk, out Vector2 stick)
    {
        long current = Volatile.Read(ref held);
        hasNunchuk = (current & NunchukPresent) != 0;
        stick = new((short)(current >> StickXShift), (short)(current >> StickYShift));
        if ((current & StickDeflected) != 0) counts[StickIndex]++;
        int limit = hasNunchuk ? StickIndex : CIndex;
        bool active = false;
        for (int bit = 0; bit < limit; bit++)
        {
            long stamp = Volatile.Read(ref lastPress[bit]);
            if ((current & (1 << bit)) != 0 || stamp != 0 && stamp >= cutoff)
            {
                counts[bit]++;
                active = true;
            }
        }
        return active;
    }
}
