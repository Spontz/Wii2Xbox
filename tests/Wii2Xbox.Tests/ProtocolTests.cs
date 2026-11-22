using Xunit;

namespace Wii2Xbox.Tests;

public sealed class ProtocolTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void PrecomputedMapPreservesEveryCoreButtonCombination(bool horizontal)
    {
        var profile = new Profile { Horizontal = horizontal };
        profile.Bindings["B"] = "RightTrigger";
        var map = new CoreInputMap(profile);
        for (int raw = 0; raw <= ushort.MaxValue; raw++)
        {
            if ((raw & 0x60e0) != 0) continue;
            var input = new WiiInput((WiiButtons)raw);
            Assert.Equal(InputMapper.Map(input, profile), map.Map(input.Buttons));
        }
    }

    [Theory]
    [InlineData(0x20, 5)]
    [InlineData(0x21, 5)]
    [InlineData(0x22, 5)]
    [InlineData(0x30, 2)]
    [InlineData(0x32, 11)]
    public void CoreModeRejectsControlAndTruncatedReports(byte reportId, int size)
    {
        var report = new byte[size];
        report[0] = reportId;
        report[1] = 0xff;
        Assert.False(InputMapper.TryParseCore(report, out _));
    }

    [Fact]
    public void MasksSensorBitsInButtonReport()
    {
        Assert.True(InputMapper.TryParseCore([0x30, 0xe0, 0x60], out var input));
        Assert.Equal(WiiButtons.None, input.Buttons);
    }

    [Theory]
    [InlineData(0x01, 2, 4)]
    [InlineData(0x02, 1, 8)]
    [InlineData(0x04, 8, 2)]
    [InlineData(0x08, 4, 1)]
    public void MapsDpadAccordingToOrientation(byte wiiDirection, ushort horizontal, ushort vertical)
    {
        Assert.True(InputMapper.TryParseCore([0x30, wiiDirection, 0], out var input));
        Assert.Equal(horizontal, InputMapper.Map(input, new Profile()).Buttons);
        Assert.Equal(vertical, InputMapper.Map(input, new Profile { Horizontal = false }).Buttons);
    }

    [Fact]
    public void CombinedPressAndReleaseProduceCompleteStates()
    {
        var profile = new Profile();
        Assert.True(InputMapper.TryParseCore([0x30, 0x10, 0x89], out var pressed));
        // Wii +, HOME, A and 2 become Xbox Start, Guide, X and A.
        Assert.Equal((ushort)0x5410, InputMapper.Map(pressed, profile).Buttons);
        Assert.True(InputMapper.TryParseCore([0x30, 0x00, 0x00], out var released));
        Assert.Equal(default, InputMapper.Map(released, profile));
    }
}
