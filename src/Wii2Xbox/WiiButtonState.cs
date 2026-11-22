using System.Numerics;

namespace Wii2Xbox;

internal sealed class WiiButtonState
{
    public const int BitCount = 16;
    private readonly long[] lastPress = new long[BitCount];
    private int held;

    public void Publish(WiiButtons buttons, long timestamp)
    {
        int next = (int)buttons;
        if (held == next) return;

        uint rising = (uint)(next & ~held);
        while (rising != 0)
        {
            Volatile.Write(ref lastPress[BitOperations.TrailingZeroCount(rising)], timestamp);
            rising &= rising - 1;
        }
        Volatile.Write(ref held, next);
    }

    public bool Accumulate(long cutoff, Span<int> counts)
    {
        int current = Volatile.Read(ref held);
        bool active = false;
        for (int bit = 0; bit < BitCount; bit++)
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
