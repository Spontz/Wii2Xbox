using HidSharp;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Wii2Xbox;

internal interface IControllerTransport : IDisposable
{
    int InputLength { get; }
    int ReadTimeout { get; set; }
    int Read(byte[] buffer);
    void Send(WiiCommand command, bool infrared = false);
    void Send(InfraredCommand command);
    void SetRumble(bool enabled) { }
}

internal sealed record WiiDevice(HidDevice Device, ulong? BluetoothAddress)
{
    public string HidPath => Device.DevicePath;
    public string Id => BluetoothAddress is ulong address ? $"bluetooth:{address:X12}" : HidPath;
    public string Model => Device.ProductID == 0x0330 ? "Wii Remote Plus" : "Wii Remote";

    public static WiiDevice[] Find() => BluetoothHidIdentity.NintendoInterfaces()
        .Select(device => new WiiDevice(device, BluetoothHidIdentity.FromInterface(device.DevicePath)))
        .DistinctBy(device => device.Id, StringComparer.OrdinalIgnoreCase).ToArray();

    public IControllerTransport Open()
    {
        if (!Device.TryOpen(out var stream))
            throw new IOException(UiText.Get("CannotOpenController"));
        try
        {
            stream.ReadTimeout = InputDeadline.LimitMs;
            stream.WriteTimeout = 500;
            return new Connection(this, stream);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    private sealed class Connection(WiiDevice device, HidStream stream) : IControllerTransport
    {
        private readonly byte[] report = new byte[Math.Max(22, device.Device.GetMaxOutputReportLength())];
        private readonly object writes = new();
        private bool disposed;
        private bool rumbleRequested;
        private bool? rumbleSent = false;
        public int InputLength => device.Device.GetMaxInputReportLength();
        public int ReadTimeout { get => stream.ReadTimeout; set => stream.ReadTimeout = value; }
        public int Read(byte[] buffer) => stream.Read(buffer, 0, buffer.Length);
        public void Dispose()
        {
            lock (writes)
            {
                if (disposed) return;
                disposed = true;
                try
                {
                    if (rumbleSent != false) WriteRumble(false);
                }
                finally { stream.Dispose(); }
            }
        }

        public void Send(WiiCommand command, bool infrared)
        {
            lock (writes)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                EncodeCommand(report, command, infrared);
                WriteReport(rumbleRequested);
            }
        }

        public void Send(InfraredCommand command)
        {
            lock (writes)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                InfraredSetup.Encode(report, command);
                WriteReport(rumbleRequested);
            }
        }

        public void SetRumble(bool enabled)
        {
            // ViGEm delivers feedback independently of the HID reader.
            lock (writes)
            {
                if (disposed) return;
                rumbleRequested = enabled;
                if (rumbleSent != enabled) WriteRumble(enabled);
            }
        }

        private void WriteRumble(bool enabled)
        {
            EncodeCommand(report, WiiCommand.Rumble);
            try
            {
                WriteReport(enabled);
            }
            catch (Exception error) when (error is IOException or TimeoutException or ObjectDisposedException)
            {
                Debug.WriteLine(error);
            }
        }

        private void WriteReport(bool rumbleEnabled)
        {
            ApplyRumble(report, rumbleEnabled);
            if (rumbleSent != rumbleEnabled) rumbleSent = null;
            device.Write(stream, report);
            rumbleSent = rumbleEnabled;
        }
    }

    private void Write(HidStream stream, byte[] report)
    {
        if (Device.ProductID == 0x0306 && TryControlReport(report))
            return;
        try
        {
            stream.Write(report);
        }
        catch (IOException) when (Device.ProductID == 0x0330 && TryControlReport(report))
        {
            // Some adapters expose the control write instead of the interrupt write.
        }
    }

    internal static void EncodeCommand(Span<byte> report, WiiCommand command, bool infrared = false)
    {
        report.Clear();
        switch (command)
        {
            case WiiCommand.Rumble:
                report[0] = 0x10;
                break;
            case WiiCommand.PlayerLed1:
            case WiiCommand.PlayerLed2:
            case WiiCommand.PlayerLed3:
            case WiiCommand.PlayerLed4:
                report[0] = 0x11;
                report[1] = (byte)(0x10 << ((int)command - (int)WiiCommand.PlayerLed1));
                break;
            case WiiCommand.Status:
                report[0] = 0x15;
                break;
            case WiiCommand.CoreMode:
            case WiiCommand.ExtensionMode:
                report[0] = 0x12;
                report[1] = 0x04; // Continuous reports.
                report[2] = infrared ? (byte)0x36 : command == WiiCommand.CoreMode ? (byte)0x30 : (byte)0x32;
                break;
            case WiiCommand.Init:
            case WiiCommand.DisableEncryption:
                report[0] = 0x16;
                report[1] = 0x04; // I²C registers.
                report[2] = 0xa4;
                report[4] = command == WiiCommand.Init ? (byte)0xf0 : (byte)0xfb;
                report[5] = 1;
                report[6] = command == WiiCommand.Init ? (byte)0x55 : (byte)0x00;
                break;
            case WiiCommand.Identify:
            case WiiCommand.ReadCalibration:
                report[0] = 0x17;
                report[1] = 0x04;
                report[2] = 0xa4;
                report[4] = command == WiiCommand.Identify ? (byte)0xfa : (byte)0x20;
                report[6] = command == WiiCommand.Identify ? (byte)6 : (byte)16;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command));
        }
    }

    internal static void ApplyRumble(Span<byte> report, bool enabled) =>
        report[1] = (byte)((report[1] & ~1) | (enabled ? 1 : 0));

    private bool TryControlReport(byte[] report)
    {
        using var handle = CreateFile(HidPath, 0xc0000000, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
        return !handle.IsInvalid && HidD_SetOutputReport(handle, report, report.Length);
    }

    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode)]
    private static extern SafeFileHandle CreateFile(string path, uint access, uint share,
        IntPtr security, uint disposition, uint flags, IntPtr template);

    [DllImport("hid.dll")]
    [return: MarshalAs(UnmanagedType.U1)]
    private static extern bool HidD_SetOutputReport(SafeFileHandle handle, byte[] report, int length);
}
