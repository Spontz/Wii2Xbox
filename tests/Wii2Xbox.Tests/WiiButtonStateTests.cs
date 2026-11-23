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
        first.Publish(new(WiiButtons.A | WiiButtons.Home), 100);
        first.Publish(new(WiiButtons.A), 110);
        second.Publish(new(WiiButtons.A | WiiButtons.Two), 120);
        int[] counts = new int[WiiButtonState.BitCount];

        first.Accumulate(90, counts, out _, out _);
        second.Accumulate(90, counts, out _, out _);
        Assert.Equal(2, Count(WiiButtons.A));
        Assert.Equal(1, Count(WiiButtons.Home));
        Assert.Equal(1, Count(WiiButtons.Two));

        Array.Clear(counts);
        first.Accumulate(130, counts, out _, out _);
        second.Accumulate(130, counts, out _, out _);
        Assert.Equal(2, Count(WiiButtons.A));
        Assert.Equal(0, Count(WiiButtons.Home));

        Array.Clear(counts);
        second.Accumulate(130, counts, out _, out _);
        Assert.Equal(1, Count(WiiButtons.A));
        Assert.Equal(1, Count(WiiButtons.Two));

        int Count(WiiButtons button) => counts[BitOperations.TrailingZeroCount((uint)button)];
    }

    [Fact]
    public void CombinesNunchukButtonsAndDropsTheirPulsesWhenUnplugged()
    {
        var first = new WiiButtonState();
        var second = new WiiButtonState();
        int[] counts = new int[WiiButtonState.BitCount];
        first.Publish(new(WiiButtons.None, C: true, Z: true, HasNunchuk: true), 100);
        first.Publish(new(WiiButtons.None, Z: true, HasNunchuk: true), 110);
        second.Publish(new(WiiButtons.None, C: true, HasNunchuk: true), 120);
        Assert.True(first.Accumulate(90, counts, out bool attached, out _));
        Assert.True(attached);
        Assert.True(second.Accumulate(90, counts, out _, out _));
        Assert.Equal(2, counts[WiiButtonState.CIndex]);
        Assert.Equal(1, counts[WiiButtonState.ZIndex]);

        first.Publish(new(WiiButtons.None), 125);
        Array.Clear(counts);
        Assert.False(first.Accumulate(90, counts, out attached, out _));
        Assert.False(attached);
        Assert.True(second.Accumulate(90, counts, out _, out _));
        Assert.Equal(1, counts[WiiButtonState.CIndex]);
        Assert.Equal(0, counts[WiiButtonState.ZIndex]);
    }

    [Fact]
    public void CombinesDeflectedSticksAndClearsThemAtCalibratedNeutralOrUnplug()
    {
        var first = new WiiButtonState();
        var second = new WiiButtonState();
        var calibration = new StickCalibration(new(40, 145, 225), new(40, 110, 225));
        WiiInput neutral = new(WiiButtons.None, StickX: 145, StickY: 110, HasNunchuk: true);
        int[] counts = new int[WiiButtonState.BitCount];

        first.Publish(neutral with { StickX = 158 }, 100, calibration);
        second.Publish(neutral with { StickY = 97 }, 100, calibration);
        CheckSticks(2, new(13, 0), new(0, -13));

        first.Publish(neutral with { StickX = 115, StickY = 132 }, 105, calibration);
        CheckSticks(2, new(-30, 22), new(0, -13));

        first.Publish(neutral with { StickX = 147 }, 110, calibration);
        CheckSticks(1, Vector2.Zero, new(0, -13));

        second.Publish(new(WiiButtons.None), 120, calibration);
        CheckSticks(0, Vector2.Zero, Vector2.Zero);

        void CheckSticks(int count, Vector2 expectedFirst, Vector2 expectedSecond)
        {
            Array.Clear(counts);
            Assert.False(first.Accumulate(90, counts, out _, out var firstStick));
            Assert.False(second.Accumulate(90, counts, out _, out var secondStick));
            Assert.Equal(count, counts[WiiButtonState.StickIndex]);
            Assert.Equal(expectedFirst, firstStick);
            Assert.Equal(expectedSecond, secondStick);
        }
    }
}
