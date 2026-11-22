using System.Globalization;
using System.Runtime.InteropServices;
using HidSharp;

namespace Wii2Xbox;

internal static class BluetoothHidIdentity
{
    internal static IEnumerable<HidDevice> NintendoInterfaces() => DeviceList.Local.GetHidDevices(0x057e)
        .Where(device => device.ProductID is 0x0306 or 0x0330)
        .DistinctBy(device => device.DevicePath, StringComparer.OrdinalIgnoreCase);

    public static ulong? FromInterface(string path) =>
        FindDevNode(path) is uint node ? FromDevNode(node) : null;

    internal static uint? FindDevNode(string path)
    {
        var set = SetupDiCreateDeviceInfoList(IntPtr.Zero, IntPtr.Zero);
        if (set == new IntPtr(-1)) return null;
        try
        {
            var deviceInterface = new InterfaceData { Size = (uint)Marshal.SizeOf<InterfaceData>() };
            if (!SetupDiOpenDeviceInterface(set, path, 0, ref deviceInterface)) return null;
            var device = new DeviceInfo { Size = (uint)Marshal.SizeOf<DeviceInfo>() };
            bool extracted = SetupDiGetDeviceInterfaceDetail(set, ref deviceInterface,
                IntPtr.Zero, 0, out _, ref device);
            if (!extracted && Marshal.GetLastWin32Error() != 122) return null;
            return device.Instance;
        }
        finally { SetupDiDestroyDeviceInfoList(set); }
    }

    internal static ulong? FromDevNode(uint node)
    {
        var id = new char[256];
        for (int depth = 0; depth < 8; depth++)
        {
            if (CM_Get_Device_ID(node, id, (uint)id.Length, 0) == 0)
            {
                int end = Array.IndexOf(id, '\0');
                var name = id.AsSpan(0, end >= 0 ? end : id.Length);
                if (ParseAddress(name) is ulong address) return address;
            }
            if (CM_Get_Parent(out uint parent, node, 0) != 0) break;
            node = parent;
        }
        return null;
    }

    internal static ulong? ParseAddress(ReadOnlySpan<char> deviceId)
    {
        const string remote = "BTHENUM\\DEV_";
        const string service = "BTHENUM\\{00001124-0000-1000-8000-00805f9b34fb}";
        ReadOnlySpan<char> address;
        if (deviceId.StartsWith(remote, StringComparison.OrdinalIgnoreCase) &&
            deviceId.Length > remote.Length + 12 && deviceId[remote.Length + 12] == '\\')
        {
            address = deviceId.Slice(remote.Length, 12);
        }
        else if (deviceId.StartsWith(service, StringComparison.OrdinalIgnoreCase))
        {
            int separator = deviceId.LastIndexOf('&');
            if (separator < service.Length || deviceId.Length < separator + 16 ||
                !deviceId.Slice(separator + 13, 2).Equals("_C", StringComparison.OrdinalIgnoreCase))
                return null;
            address = deviceId.Slice(separator + 1, 12);
        }
        else return null;

        return ulong.TryParse(address, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value) &&
            value != 0 ? value : null;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct InterfaceData { public uint Size; public Guid Class; public uint Flags; public UIntPtr Reserved; }

    [StructLayout(LayoutKind.Sequential)]
    private struct DeviceInfo { public uint Size; public Guid Class; public uint Instance; public UIntPtr Reserved; }

    [DllImport("setupapi.dll", ExactSpelling = true)]
    private static extern IntPtr SetupDiCreateDeviceInfoList(IntPtr classGuid, IntPtr parent);
    [DllImport("setupapi.dll", EntryPoint = "SetupDiOpenDeviceInterfaceW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern bool SetupDiOpenDeviceInterface(IntPtr set, string path, uint flags, ref InterfaceData data);
    [DllImport("setupapi.dll", EntryPoint = "SetupDiGetDeviceInterfaceDetailW", ExactSpelling = true, SetLastError = true)]
    private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr set, ref InterfaceData data,
        IntPtr detail, uint size, out uint required, ref DeviceInfo device);
    [DllImport("setupapi.dll", ExactSpelling = true)]
    private static extern bool SetupDiDestroyDeviceInfoList(IntPtr set);
    [DllImport("cfgmgr32.dll", EntryPoint = "CM_Get_Device_IDW", CharSet = CharSet.Unicode, ExactSpelling = true)]
    private static extern uint CM_Get_Device_ID(uint node, [Out] char[] id, uint length, uint flags);
    [DllImport("cfgmgr32.dll", ExactSpelling = true)]
    private static extern uint CM_Get_Parent(out uint parent, uint node, uint flags);
}
