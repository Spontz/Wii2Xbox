using HidSharp;
using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

namespace Wii2Xbox;

internal interface IControllerTransport : IDisposable
{
    int InputLength { get; }
    int ReadTimeout { get; set; }
    int Read(byte[] buffer);
    void Send(WiiCommand command, bool infrared = false);
    void Send(InfraredCommand command);
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
            throw new IOException("Windows no ha podido abrir el mando. Comprueba que esté encendido y cierra otras aplicaciones que lo utilicen.");
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
        private int disposed;
        public int InputLength => device.Device.GetMaxInputReportLength();
        public int ReadTimeout { get => stream.ReadTimeout; set => stream.ReadTimeout = value; }
        public int Read(byte[] buffer) => stream.Read(buffer, 0, buffer.Length);
        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposed, 1) == 0) stream.Dispose();
        }

        public void Send(WiiCommand command, bool infrared)
        {
            EncodeCommand(report, command, infrared);
            device.Write(stream, report);
        }

        public void Send(InfraredCommand command)
        {
            InfraredSetup.Encode(report, command);
            device.Write(stream, report);
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
