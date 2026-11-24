using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Wii2Xbox;

internal static class BluetoothService
{
    private const int NoMoreItems = 259;
    private const int MoreData = 234;
    private const int ServiceAlreadyEnabled = 87;
    private const int NotFound = 1168;
    private static readonly Guid HidService = new("00001124-0000-1000-8000-00805f9b34fb");

    internal enum DeviceKind { Unknown, Phone, Computer, Display, Headphones, Speaker, Gamepad, Keyboard, Mouse }

    internal sealed record Candidate(ulong RadioAddress, DeviceInfo Device)
    {
        public bool CanPair => IsSupportedModel(Device.Name);
        public bool CanTryPair => CanPair || !IsWiiFamily(Device.Name) && IsPossibleController(Device.ClassOfDevice);
        public DeviceKind Kind => CanPair ? DeviceKind.Gamepad : ClassifyDevice(Device.ClassOfDevice);
        public string DisplayName
        {
            get
            {
                string name = string.IsNullOrWhiteSpace(Device.Name) ? UiText.Get("UnnamedBluetoothDevice") : Device.Name;
                return !CanTryPair && IsWiiFamily(Device.Name) ? name + UiText.Get("UnsupportedControllerSuffix") : name;
            }
        }
        public override string ToString() => $"{DisplayName} · {Device.Address:X12}";
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

    private static bool IsPossibleController(uint deviceClass)
    {
        uint major = (deviceClass >> 8) & 0x1f;
        uint minor = (deviceClass >> 2) & 0x3f;
        // Accept uncategorized devices and the peripheral types used by joystick, gamepad, or wand hardware.
        return major is 0 or 0x1f || major == 5 && minor is 0 or 1 or 2 or 3 or 9;
    }

    private static DeviceKind ClassifyDevice(uint deviceClass)
    {
        uint major = (deviceClass >> 8) & 0x1f;
        uint minor = (deviceClass >> 2) & 0x3f;
        return (major, minor) switch
        {
            (1, _) => DeviceKind.Computer,
            (2, _) => DeviceKind.Phone,
            (4, 9 or 14 or 15) => DeviceKind.Display,
            (4, 1 or 2 or 6) => DeviceKind.Headphones,
            (4, 5 or 7 or 8 or 10) => DeviceKind.Speaker,
            (4, 18) or (5, 1 or 2 or 3 or 9) => DeviceKind.Gamepad,
            (5, _) when (minor & 0x10) != 0 => DeviceKind.Keyboard,
            (5, _) when (minor & 0x20) != 0 => DeviceKind.Mouse,
            _ => DeviceKind.Unknown
        };
    }

    public static Candidate[] Find(CancellationToken cancellation, bool showAll = false)
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
                TimeoutMultiplier = showAll ? (byte)3 : (byte)2, // Inquiry duration in 1.28-second units.
                Radio = radio.DangerousGetHandle()
            };
            var device = EmptyDevice();
            var recentlySeen = DateTime.UtcNow.AddSeconds(-1);
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
                    if (showAll ? device.LastSeen.IsAtOrAfter(recentlySeen) : IsWiiFamily(device.Name))
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

    public static bool Pair(Candidate candidate, bool afterRemovingPairing = false, bool tryUnknown = false)
    {
        if (!candidate.CanPair && !(tryUnknown && candidate.CanTryPair))
            throw new NotSupportedException(UiText.Get("UnsupportedWiiModel"));

        foreach (var radio in Radios())
        {
            if (ReadRadio(radio).Address != candidate.RadioAddress) continue;
            var device = EmptyDevice();
            device.Address = candidate.Device.Address;
            if (afterRemovingPairing)
                device.Name = candidate.Device.Name;
            else if (BluetoothGetDeviceInfo(radio, ref device) != 0)
                device = candidate.Device;
            bool alreadyKnown = device.Remembered != 0 || device.Authenticated != 0;

            bool authenticatedHere = false;
            try
            {
                if (device.Authenticated == 0)
                {
                    // Red SYNC uses the Bluetooth radio's raw address bytes, least significant first.
                    var pin = new ushort[6];
                    for (int index = 0; index < pin.Length; index++)
                        pin[index] = (ushort)((candidate.RadioAddress >> (8 * index)) & 0xff);

                    Check(BluetoothAuthenticateDevice(IntPtr.Zero, radio, ref device, pin, (uint)pin.Length), UiText.Get("BluetoothPairOperation"));
                    authenticatedHere = true;
                    uint serviceCount = 0;
                    int result = BluetoothEnumerateInstalledServices(radio, ref device, ref serviceCount, null);
                    if (result != MoreData) Check(result, UiText.Get("BluetoothServicesOperation"));
                }
                Check(EnableHid(radio, ref device), UiText.Get("BluetoothHidOperation"));
            }
            catch when (tryUnknown && !alreadyKnown && authenticatedHere)
            {
                ForgetPairing(candidate.Device.Address);
                throw;
            }
            return alreadyKnown;
        }
        throw new InvalidOperationException(UiText.Get("BluetoothRadioDisconnected"));
    }

    public static bool RemoveDisconnectedPairing(Candidate candidate)
    {
        if (!candidate.CanPair) return false;
        foreach (var radio in Radios())
        {
            if (ReadRadio(radio).Address != candidate.RadioAddress) continue;
            var device = EmptyDevice();
            device.Address = candidate.Device.Address;
            if (BluetoothGetDeviceInfo(radio, ref device) != 0 || device.Connected != 0 ||
                (device.Remembered == 0 && device.Authenticated == 0))
                return false;

            ForgetPairing(device.Address);
            return true;
        }
        return false;
    }

    public static void ForgetPairing(ulong address)
    {
        int result = BluetoothRemoveDevice(in address);
        if (result != NotFound) Check(result, UiText.Get("BluetoothRemovePairingOperation"));
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
            if (error is not (NoMoreItems or NotFound)) Check(error, UiText.Get("OpenBluetoothRadioOperation"));
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

        public readonly bool IsAtOrAfter(DateTime cutoff)
        {
            if (Year == 0) return false;
            return new DateTime(Year, Month, Day, Hour, Minute, Second, Milliseconds, DateTimeKind.Utc) >= cutoff;
        }
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
    [DllImport(Library)] private static extern int BluetoothRemoveDevice(in ulong address);
    [DllImport(Library)] private static extern int BluetoothAuthenticateDevice(IntPtr parent, SafeFileHandle radio, ref DeviceInfo device, [In] ushort[] pin, uint length);
    [DllImport(Library)] private static extern int BluetoothEnumerateInstalledServices(SafeFileHandle radio, ref DeviceInfo device, ref uint count, [Out, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] Guid[]? services);
    [DllImport(Library)] private static extern int BluetoothSetServiceState(SafeFileHandle radio, ref DeviceInfo device, in Guid service, uint flags);
}
