using Xunit;

namespace Wii2Xbox.Tests;

public sealed class BluetoothIdentityTests
{
    [Theory]
    [InlineData("Nintendo RVL-CNT-01", true, true)]
    [InlineData(" nintendo rvl-cnt-01-tr ", true, true)]
    [InlineData("Nintendo RVL-WBC-01", true, false)]
    [InlineData("Nintendo RVL_WBC-01", true, false)]
    [InlineData("Nintendo RVL-CNT-EXAMPLE", true, false)]
    [InlineData("Xbox Wireless Controller", false, false)]
    [InlineData(null, false, false)]
    public void DistinguishesDiscoverableWiiModelsFromPairableRemotes(string? name, bool family, bool pairable)
    {
        Assert.Equal(family, BluetoothService.IsWiiFamily(name));
        Assert.Equal(pairable, BluetoothService.IsSupportedModel(name));
    }

    [Fact]
    public void PairingRefusesBalanceBoardBeforeUsingTheRadio()
    {
        var balanceBoard = new BluetoothService.Candidate(0, new BluetoothService.DeviceInfo
        {
            Name = "Nintendo RVL-WBC-01", Address = 0xaa11223344bb
        });
        Assert.Contains("unsupported", balanceBoard.ToString());
        Assert.Throws<NotSupportedException>(() => BluetoothService.Pair(balanceBoard));
        Assert.Throws<NotSupportedException>(() => BluetoothService.Pair(balanceBoard, tryUnknown: true));
    }

    [Theory]
    [InlineData("Generic Wii Controller", 0x0508, true)]
    [InlineData("Generic joystick", 0x0504, true)]
    [InlineData("Headphones", 0x0404, false)]
    [InlineData("Keyboard", 0x0540, false)]
    [InlineData("Nintendo RVL-WBC-01", 0x0508, false)]
    public void OnlyPlausibleControllersMayBeTriedWithAnUnknownName(string name, uint deviceClass, bool allowed)
    {
        var candidate = new BluetoothService.Candidate(0, new BluetoothService.DeviceInfo
        {
            Name = name, ClassOfDevice = deviceClass
        });
        Assert.Equal(allowed, candidate.CanTryPair);
        if (!allowed)
            Assert.Throws<NotSupportedException>(() => BluetoothService.Pair(candidate, tryUnknown: true));
    }

    [Fact]
    public void RecentInquiryRejectsOldOrMissingSightings()
    {
        var cutoff = new DateTime(2026, 11, 23, 15, 42, 0, DateTimeKind.Utc);
        var seen = new BluetoothService.SystemTime { Year = 2026, Month = 11, Day = 23, Hour = 15, Minute = 42 };
        Assert.True(seen.IsAtOrAfter(cutoff));
        Assert.False(seen.IsAtOrAfter(cutoff.AddSeconds(1)));
        Assert.False(default(BluetoothService.SystemTime).IsAtOrAfter(cutoff));
    }

    [Theory]
    [InlineData(@"BTHENUM\DEV_AA11223344BB\7&SOME&BLUETOOTHDEVICE_AA11223344BB")]
    [InlineData(@"BTHENUM\{00001124-0000-1000-8000-00805F9B34FB}_VID&0002057e_PID&0306\7&EXAMPLE&0&aa11223344bb_C00000000")]
    public void RecognizesRemoteAddressFromBluetoothDeviceAndHidService(string id) =>
        Assert.Equal(0xaa11223344bbUL, BluetoothHidIdentity.ParseAddress(id));

    [Theory]
    [InlineData(@"USB\VID_057E&PID_0306\AA11223344BB")]
    [InlineData(@"BTHENUM\DEV_000000000000\anything")]
    [InlineData(@"BTHENUM\DEV_NOTANADDRESS\anything")]
    [InlineData(@"BTHENUM\{0000110c-0000-1000-8000-00805F9B34FB}\7&0&AA11223344BB_C00000000")]
    public void RejectsIdsWithoutARealBluetoothHidIdentity(string id) =>
        Assert.Null(BluetoothHidIdentity.ParseAddress(id));
}
