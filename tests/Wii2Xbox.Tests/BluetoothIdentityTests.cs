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
