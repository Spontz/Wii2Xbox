using System.Diagnostics;
using Xunit;

namespace Wii2Xbox.Tests;

public sealed class NunchukTests
{
    [Fact]
    public void NegotiatesCalibrationAndReleasesOnlyExtensionWhenUnplugged()
    {
        var protocol = new NunchukProtocol();
        Initialize(protocol);
        AssertCommand(protocol, [0x21, 0, 0, 0x50, 0, 0xfa, 0, 0, 0xa4, 0x20, 0, 0], WiiCommand.ReadCalibration);
        AssertCommand(protocol, [0x21, 0, 0, 0xf0, 0, 0x20, 0, 0, 0, 0, 0, 0, 0, 0, 225, 40, 130, 204, 50, 120, 0, 0], WiiCommand.ExtensionMode);
        Assert.False(protocol.UsingFallbackCalibration);
        var map = new CoreInputMap(new Profile { Deadzone = 8 });
        Assert.True(protocol.Process([0x32, 0, 8, 225, 50, 0, 0, 0, 0, 0, 0], 0, out var state, out _));
        var xbox = map.Map(state, protocol.Calibration);
        Assert.Equal(short.MaxValue, xbox.X);
        Assert.Equal(short.MinValue, xbox.Y);
        Assert.Equal((byte)255, xbox.LeftTrigger);
        Assert.Equal((ushort)0x4100, xbox.Buttons);
        var combined = new byte[22];
        combined[0] = 0x36;
        combined[2] = 8;
        combined[13] = 225;
        combined[14] = 50;
        Assert.True(protocol.Process(combined, 0, out var combinedState, out _));
        Assert.Equal(xbox, map.Map(combinedState, protocol.Calibration));
        Assert.True(protocol.Process([0x20, 0, 8, 0, 0, 0, 0], 0, out state, out var command));
        Assert.Equal(WiiCommand.CoreMode, command);
        Assert.Equal(new XboxInput(0x4000, 0, 0, 0, 0), map.Map(state, protocol.Calibration));
        Assert.True(protocol.Process([0x32, 0, 8, 225, 50, 0, 0, 0, 0, 0, 0], 0, out state, out _));
        Assert.False(state.HasNunchuk);
    }

    [Fact]
    public void UnknownAccessoryDoesNotGenerateAnalogOrExtensionButtons()
    {
        var protocol = new NunchukProtocol();
        Initialize(protocol);
        AssertCommand(protocol, [0x21, 0, 0, 0x50, 0, 0xfa, 0, 0, 0xa4, 0x20, 1, 1], WiiCommand.CoreMode);
        Assert.True(protocol.Process([0x32, 0, 0, 255, 255, 0, 0, 0, 0, 0, 0], 0, out var state, out _));
        Assert.False(state.HasNunchuk);
    }

    [Fact]
    public void MissingCalibrationUsesFallbackAndHonorsDeadzone()
    {
        var protocol = new NunchukProtocol();
        Initialize(protocol);
        AssertCommand(protocol, [0x21, 0, 0, 0x50, 0, 0xfa, 0, 0, 0xa4, 0x20, 0, 0], WiiCommand.ReadCalibration);
        Assert.True(protocol.Process([0x30, 0, 0], Stopwatch.Frequency, out _, out var command));
        Assert.Equal(WiiCommand.ExtensionMode, command);
        Assert.True(protocol.UsingFallbackCalibration);
        Assert.True(protocol.Process([0x32, 0, 0, 131, 123, 0, 0, 0, 3, 0, 0], Stopwatch.Frequency, out var state, out _));
        Assert.True(state.HasNunchuk);
        Assert.Equal(default, new CoreInputMap(new Profile()).Map(state, protocol.Calibration));
    }

    [Fact]
    public void ReadAndWriteRegistersArePackedWithoutLingeringData()
    {
        var output = new byte[22];
        WiiDevice.EncodeCommand(output, WiiCommand.Init);
        Assert.Equal(new byte[] { 0x16, 4, 0xa4, 0, 0xf0, 1, 0x55 }, output[..7]);
        WiiDevice.EncodeCommand(output, WiiCommand.ReadCalibration);
        Assert.Equal(new byte[] { 0x17, 4, 0xa4, 0, 0x20, 0, 16 }, output[..7]);
        WiiDevice.EncodeCommand(output, WiiCommand.ExtensionMode);
        Assert.Equal(new byte[] { 0x12, 4, 0x32 }, output[..3]);
        Assert.All(output[3..], value => Assert.Equal(0, value));
    }

    private static void Initialize(NunchukProtocol protocol)
    {
        AssertCommand(protocol, [0x20, 0, 0, 2, 0, 0, 0], WiiCommand.CoreMode);
        AssertCommand(protocol, [0x30, 0, 0], WiiCommand.Init);
        AssertCommand(protocol, [0x22, 0, 0, 0x12, 0], WiiCommand.None);
        AssertCommand(protocol, [0x22, 0, 0, 0x16, 0], WiiCommand.DisableEncryption);
        AssertCommand(protocol, [0x22, 0, 0, 0x16, 0], WiiCommand.Identify);
    }

    private static void AssertCommand(NunchukProtocol protocol, byte[] report, WiiCommand expected)
    {
        protocol.Process(report, 0, out _, out var command);
        Assert.Equal(expected, command);
    }
}
