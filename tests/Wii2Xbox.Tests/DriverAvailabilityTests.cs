using Nefarius.ViGEm.Client.Exceptions;
using Xunit;

namespace Wii2Xbox.Tests;

public sealed class DriverAvailabilityTests
{
    [Theory]
    [InlineData(typeof(VigemBusNotFoundException), "was not found", true)]
    [InlineData(typeof(VigemBusVersionMismatchException), "unsupported", true)]
    [InlineData(typeof(VigemBusAccessFailedException), "denied access", false)]
    public void ConvertsDriverFailuresIntoDifferentActions(Type type, string expected, bool canInstall)
    {
        var result = XboxOutput.CheckDriver(() => throw (Exception)Activator.CreateInstance(type)!);
        Assert.False(result.Ready);
        Assert.Equal(canInstall, result.CanInstall);
        Assert.Contains(expected, result.Message);
    }

    [Fact]
    public void TheDriverCheckReleasesItsConnection()
    {
        var bus = new TestConnection();
        var result = XboxOutput.CheckDriver(() => bus);
        Assert.True(result.Ready);
        Assert.False(result.CanInstall);
        Assert.True(bus.Closed);
    }

    private sealed class TestConnection : IDisposable
    {
        public bool Closed { get; private set; }
        public void Dispose() => Closed = true;
    }
}
