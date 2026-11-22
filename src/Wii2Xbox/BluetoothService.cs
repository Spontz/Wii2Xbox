using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Wii2Xbox;

internal static class BluetoothService
{
    private const int NoMoreItems = 259;
    private const int MoreData = 234;
    private const int ServiceAlreadyEnabled = 87;
    private static readonly Guid HidService = new("00001124-0000-1000-8000-00805f9b34fb");

    internal sealed record Candidate(ulong RadioAddress, DeviceInfo Device)
    {
        public bool CanPair => IsSupportedModel(Device.Name);
        public override string ToString() =>
            $"{Device.Name} · {Device.Address:X12}{(CanPair ? "" : UiText.Get("UnsupportedControllerSuffix"))}";
    }

    internal static bool IsSupportedModel(string? name)
    {
        var normalized = name?.Trim();
        return string.Equals(normalized, "Nintendo RVL-CNT-01", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "Nintendo RVL-CNT-01-TR", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool IsWiiFamily(string? name)
    {
        var normalized = name?.TrimStart();
        return normalized?.StartsWith("Nintendo RVL-", StringComparison.OrdinalIgnoreCase) == true ||
            normalized?.StartsWith("Nintendo RVL_", StringComparison.OrdinalIgnoreCase) == true;
    }

    public static Candidate[] Find(CancellationToken cancellation)
    {
        var found = new Dictionary<ulong, Candidate>();
        bool hasRadio = false;
        foreach (var radio in Radios())
        {
            cancellation.ThrowIfCancellationRequested();
            hasRadio = true;
            var info = ReadRadio(radio);
            var search = new SearchParams
            {
                Size = SizeOf<SearchParams>(),
                ReturnUnknown = 1,
                ReturnRemembered = 1,
                ReturnAuthenticated = 1,
                ReturnConnected = 1,
                IssueInquiry = 1,
                TimeoutMultiplier = 2, // 2.56 seconds per radio.
                Radio = radio.DangerousGetHandle()
            };
            var device = EmptyDevice();
            var find = BluetoothFindFirstDevice(ref search, ref device);
            if (find == IntPtr.Zero)
            {
                var error = Marshal.GetLastWin32Error();
                cancellation.ThrowIfCancellationRequested();
                if (error != NoMoreItems) Check(error, UiText.Get("BluetoothSearchOperation"));
                continue;
            }
            try
            {
                do
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (IsWiiFamily(device.Name))
                        found.TryAdd(device.Address, new Candidate(info.Address, device));
                } while (BluetoothFindNextDevice(find, ref device));
                var error = Marshal.GetLastWin32Error();
                if (error != NoMoreItems) Check(error, UiText.Get("BluetoothEnumerateOperation"));
            }
            finally { BluetoothFindDeviceClose(find); }
        }
        cancellation.ThrowIfCancellationRequested();
        if (!hasRadio) throw new InvalidOperationException(UiText.Get("NoBluetoothRadio"));
        return found.Values.ToArray();
    }

    public static void Pair(Candidate candidate, bool syncButton)
    {
        if (!candidate.CanPair)
            throw new NotSupportedException(UiText.Get("UnsupportedWiiModel"));

        foreach (var radio in Radios())
        {
            if (ReadRadio(radio).Address != candidate.RadioAddress) continue;
            var device = EmptyDevice();
            device.Address = candidate.Device.Address;
            var getInfo = BluetoothGetDeviceInfo(radio, ref device);
            if (getInfo != 0) device = candidate.Device;

            if (device.Authenticated == 0)
            {
                // Wii Remote PIN is the six raw address bytes, least significant first.
                ulong address = syncButton ? candidate.RadioAddress : device.Address;
                var pin = new ushort[6];
                for (int index = 0; index < pin.Length; index++)
                    pin[index] = (ushort)((address >> (8 * index)) & 0xff);

                Check(BluetoothAuthenticateDevice(IntPtr.Zero, radio, ref device, pin, (uint)pin.Length), UiText.Get("BluetoothPairOperation"));
                uint serviceCount = 0;
                int result = BluetoothEnumerateInstalledServices(radio, ref device, ref serviceCount, null);
                if (result != MoreData) Check(result, UiText.Get("BluetoothServicesOperation"));
            }
            Check(EnableHid(radio, ref device), UiText.Get("BluetoothHidOperation"));
            return;
        }
        throw new InvalidOperationException(UiText.Get("BluetoothRadioDisconnected"));
    }

    private static int EnableHid(SafeFileHandle radio, ref DeviceInfo device)
    {
        var service = HidService;
        int result = BluetoothSetServiceState(radio, ref device, in service, 1);
        if (result != ServiceAlreadyEnabled) return result;

        // ERROR_INVALID_PARAMETER is also documented for an already enabled service.
        // Confirm the installed service before treating that result as success.
        uint count = 0;
        if (BluetoothEnumerateInstalledServices(radio, ref device, ref count, null) != MoreData || count is 0 or > 256)
            return result;
        var installed = new Guid[count];
        return BluetoothEnumerateInstalledServices(radio, ref device, ref count, installed) == 0 &&
               installed.Contains(service) ? 0 : result;
    }

    private static IEnumerable<SafeFileHandle> Radios()
    {
        uint size = sizeof(uint);
        var find = BluetoothFindFirstRadio(ref size, out var handle);
        if (find == IntPtr.Zero)
        {
            var error = Marshal.GetLastWin32Error();
            handle?.Dispose();
            if (error is not (NoMoreItems or 1168)) Check(error, UiText.Get("OpenBluetoothRadioOperation"));
            yield break;
        }
        try
        {
            while (true)
            {
                using (handle) yield return handle;
                if (!BluetoothFindNextRadio(find, out handle))
                {
                    var error = Marshal.GetLastWin32Error();
                    handle?.Dispose();
                    if (error != NoMoreItems) Check(error, UiText.Get("EnumerateBluetoothRadiosOperation"));
                    break;
                }
            }
        }
        finally { BluetoothFindRadioClose(find); }
    }

    private static RadioInfo ReadRadio(SafeFileHandle radio)
    {
        var info = new RadioInfo { Size = SizeOf<RadioInfo>(), Name = "" };
        Check(BluetoothGetRadioInfo(radio, ref info), UiText.Get("QueryBluetoothRadioOperation"));
        return info;
    }

    private static DeviceInfo EmptyDevice() => new() { Size = SizeOf<DeviceInfo>(), Name = "" };
    private static uint SizeOf<T>() => (uint)Marshal.SizeOf<T>();

    private static void Check(int error, string operation)
    {
        if (error != 0)
            throw new Win32Exception(error, UiText.Format("BluetoothOperationFailed", operation, new Win32Exception(error).Message, error));
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SearchParams
    {
        public uint Size;
        public int ReturnAuthenticated, ReturnRemembered, ReturnUnknown, ReturnConnected, IssueInquiry;
        public byte TimeoutMultiplier;
        public IntPtr Radio;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct RadioInfo
    {
        public uint Size;
        public ulong Address;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)] public string Name;
        public uint ClassOfDevice;
        public ushort LmpSubversion, Manufacturer;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct DeviceInfo
    {
        public uint Size;
        public ulong Address;
        public uint ClassOfDevice;
        public int Connected, Remembered, Authenticated;
        public SystemTime LastSeen, LastUsed;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 248)] public string Name;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct SystemTime
    {
        public ushort Year, Month, DayOfWeek, Day, Hour, Minute, Second, Milliseconds;
    }

    private const string Library = "bthprops.cpl";
    [DllImport(Library, SetLastError = true)] private static extern IntPtr BluetoothFindFirstRadio(ref uint size, out SafeFileHandle radio);
    [DllImport(Library, SetLastError = true)] private static extern bool BluetoothFindNextRadio(IntPtr find, out SafeFileHandle radio);
    [DllImport(Library)] private static extern bool BluetoothFindRadioClose(IntPtr find);
    [DllImport(Library)] private static extern int BluetoothGetRadioInfo(SafeFileHandle radio, ref RadioInfo info);
    [DllImport(Library, SetLastError = true)] private static extern IntPtr BluetoothFindFirstDevice(ref SearchParams search, ref DeviceInfo device);
    [DllImport(Library, SetLastError = true)] private static extern bool BluetoothFindNextDevice(IntPtr find, ref DeviceInfo device);
    [DllImport(Library)] private static extern bool BluetoothFindDeviceClose(IntPtr find);
    [DllImport(Library)] private static extern int BluetoothGetDeviceInfo(SafeFileHandle radio, ref DeviceInfo device);
    [DllImport(Library)] private static extern int BluetoothAuthenticateDevice(IntPtr parent, SafeFileHandle radio, ref DeviceInfo device, [In] ushort[] pin, uint length);
    [DllImport(Library)] private static extern int BluetoothEnumerateInstalledServices(SafeFileHandle radio, ref DeviceInfo device, ref uint count, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] Guid[]? services);
    [DllImport(Library)] private static extern int BluetoothSetServiceState(SafeFileHandle radio, ref DeviceInfo device, in Guid service, uint flags);
}
