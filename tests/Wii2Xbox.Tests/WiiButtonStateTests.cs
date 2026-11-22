using System.Numerics;
using Xunit;

namespace Wii2Xbox.Tests;

public sealed class WiiButtonStateTests
{
    [Fact]
    public void CombinesControllersAndRetainsShortPulses()
    {
        var first = new WiiButtonState();
        var second = new WiiButtonState();
        first.Publish(WiiButtons.A | WiiButtons.Home, 100);
        first.Publish(WiiButtons.A, 110);
        second.Publish(WiiButtons.A | WiiButtons.Two, 120);
        int[] counts = new int[WiiButtonState.BitCount];

        first.Accumulate(90, counts);
        second.Accumulate(90, counts);
        Assert.Equal(2, Count(WiiButtons.A));
        Assert.Equal(1, Count(WiiButtons.Home));
        Assert.Equal(1, Count(WiiButtons.Two));

        Array.Clear(counts);
        first.Accumulate(130, counts);
        second.Accumulate(130, counts);
        Assert.Equal(2, Count(WiiButtons.A));
        Assert.Equal(0, Count(WiiButtons.Home));

        Array.Clear(counts);
        second.Accumulate(130, counts);
        Assert.Equal(1, Count(WiiButtons.A));
        Assert.Equal(1, Count(WiiButtons.Two));

        int Count(WiiButtons button) => counts[BitOperations.TrailingZeroCount((uint)button)];
    }
}
