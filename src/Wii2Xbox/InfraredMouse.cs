using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Wii2Xbox;

internal enum InfraredCommand { Clock, Camera, Enable, Sensitivity1, Sensitivity2, BasicMode, Finish }

internal sealed class InfraredSetup
{
    private InfraredCommand command;
    private long deadline;
    public bool Pending { get; private set; } = true;
    public bool Enabled { get; private set; }

    public InfraredSetup(long now) => deadline = now + Stopwatch.Frequency / 2;

    public InfraredCommand? Process(ReadOnlySpan<byte> report, long now)
    {
        if (!Pending) return null;
        if (now >= deadline)
        {
            Pending = false;
            return null;
        }
        byte expected = command switch { InfraredCommand.Clock => 0x13, InfraredCommand.Camera => 0x1a, _ => 0x16 };
        if (report.Length < 5 || report[0] != 0x22 || report[3] != expected) return null;
        if (report[4] != 0 || command == InfraredCommand.Finish)
        {
            Pending = false;
            Enabled = report[4] == 0;
            return null;
        }
        deadline = now + Stopwatch.Frequency / 2;
        return ++command;
    }

    public static void Encode(Span<byte> report, InfraredCommand command)
    {
        report.Clear();
        if (command is InfraredCommand.Clock or InfraredCommand.Camera)
        {
            report[0] = command == InfraredCommand.Clock ? (byte)0x13 : (byte)0x1a;
            report[1] = 0x06; // Enable and acknowledge.
            return;
        }

        report[0] = 0x16;
        report[1] = 0x04;
        report[2] = 0xb0;
        var data = command switch
        {
            InfraredCommand.Enable or InfraredCommand.Finish => report[6..7],
            InfraredCommand.Sensitivity1 => report[6..15],
            InfraredCommand.Sensitivity2 => report[6..8],
            InfraredCommand.BasicMode => report[6..7],
            _ => throw new ArgumentOutOfRangeException(nameof(command))
        };
        report[4] = command switch
        {
            InfraredCommand.Enable or InfraredCommand.Finish => 0x30,
            InfraredCommand.Sensitivity1 => 0,
            InfraredCommand.Sensitivity2 => 0x1a,
            _ => 0x33
        };
        report[5] = (byte)data.Length;
        switch (command)
        {
            case InfraredCommand.Enable:
            case InfraredCommand.Finish:
                data[0] = 8;
                break;
            case InfraredCommand.Sensitivity1:
                ReadOnlySpan<byte> sensitivity1 = [0x02, 0, 0, 0x71, 1, 0, 0xc8, 0, 0x36];
                sensitivity1.CopyTo(data);
                break;
            case InfraredCommand.Sensitivity2:
                data[0] = 0x35;
                data[1] = 0x03;
                break;
            case InfraredCommand.BasicMode:
                data[0] = 1;
                break;
        }
    }
}

public sealed record InfraredCalibration(int Left, int Top, int Right, int Bottom)
{
    internal static InfraredCalibration Default { get; } = new(100, 70, 923, 697);

    internal bool IsValid =>
        Left is >= -128 and <= 1151 && Right is >= -128 and <= 1151 &&
        Top is >= -96 and <= 863 && Bottom is >= -96 and <= 863 &&
        Math.Abs(Right - Left) >= 64 && Math.Abs(Bottom - Top) >= 64;

    internal static InfraredCalibration? FromTargets(Point topLeft, Point bottomRight)
    {
        int dx = bottomRight.X - topLeft.X;
        int dy = bottomRight.Y - topLeft.Y;
        // Targets are at 1/12 and 11/12 of the screen. Extend each edge by
        // one tenth of the measured span to cover the entire display.
        var result = new InfraredCalibration(topLeft.X - dx / 10, topLeft.Y - dy / 10,
            bottomRight.X + dx / 10, bottomRight.Y + dy / 10);
        return result.IsValid ? result : null;
    }

    internal Point Map(Point aim) => new(Scale(aim.X, Left, Right), Scale(aim.Y, Top, Bottom));

    private static int Scale(int value, int start, int end) =>
        Math.Clamp((value - start) * 65535 / (end - start), 0, 65535);
}

internal sealed class InfraredCalibrationCapture
{
    private bool armed;
    private int captured;
    private int tracking;

    public bool Tracking => Volatile.Read(ref tracking) != 0;

    public void Observe(Point? aim, WiiButtons buttons)
    {
        Volatile.Write(ref tracking, aim.HasValue ? 1 : 0);
        if ((buttons & WiiButtons.A) == 0)
        {
            armed = true;
            return;
        }
        if (!armed) return;
        armed = false;
        if (aim is { } point)
            Interlocked.CompareExchange(ref captured, 1 + (point.Y << 10 | point.X), 0);
    }

    public Point? Take()
    {
        int value = Interlocked.Exchange(ref captured, 0) - 1;
        return value < 0 ? null : new Point(value & 1023, value >> 10);
    }
}

internal static class InfraredPointer
{
    public static bool TryPosition(ReadOnlySpan<byte> report, out Point position)
    {
        position = default;
        if (!TryAim(report, out var aim)) return false;
        position = InfraredCalibration.Default.Map(aim);
        return true;
    }

    public static bool TryAim(ReadOnlySpan<byte> report, out Point aim)
    {
        aim = default;
        if (report.Length < 22 || report[0] != 0x36) return false;
        var data = report.Slice(3, 10);
        Span<Point> points = stackalloc Point[4];
        for (int pair = 0; pair < 2; pair++)
        {
            int i = pair * 5;
            int packed = data[i + 2];
            points[pair * 2] = new(data[i] | ((packed & 0x30) << 4), data[i + 1] | ((packed & 0xc0) << 2));
            points[pair * 2 + 1] = new(data[i + 3] | ((packed & 3) << 8), data[i + 4] | ((packed & 0x0c) << 6));
        }

        int separation = 24;
        for (int first = 0; first < points.Length - 1; first++)
        for (int second = first + 1; second < points.Length; second++)
        {
            var a = points[first];
            var b = points[second];
            if (a.Y > 767 || b.Y > 767) continue;
            int distance = Math.Abs(a.X - b.X);
            if (distance <= separation || Math.Abs(a.Y - b.Y) > distance) continue;
            separation = distance;
            int x = (a.X + b.X) / 2;
            int y = (a.Y + b.Y) / 2;
            aim = new(1023 - x, 767 - y);
        }
        return separation > 24;
    }

}

internal sealed class InfraredMouse(Func<uint, Point, bool>? inject = null) : IDisposable
{
    private static readonly int InputSize = Marshal.SizeOf<NativeInput>();
    private bool left;
    private bool right;
    private bool armed;
    private Point? lastPosition;
    private readonly Func<uint, Point, bool> inject = inject ?? Send;
    public bool Tracking => lastPosition.HasValue;

    public void Update(ReadOnlySpan<byte> report, WiiButtons buttons,
        InfraredCalibration? calibration = null, InfraredCalibrationCapture? capture = null)
    {
        bool visible = InfraredPointer.TryAim(report, out var aim);
        if (capture is not null)
        {
            Release();
            capture.Observe(visible ? aim : null, buttons);
            return;
        }
        if (!visible)
        {
            Release();
            return;
        }
        var position = (calibration ?? InfraredCalibration.Default).Map(aim);
        bool wantsLeft = (buttons & WiiButtons.A) != 0;
        bool wantsRight = (buttons & WiiButtons.B) != 0;
        if (!wantsLeft && !wantsRight) armed = true;
        wantsLeft &= armed;
        wantsRight &= armed;
        uint flags = lastPosition != position ? 0x8001u : 0u; // ABSOLUTE | MOVE, primary display.
        if (left != wantsLeft) flags |= wantsLeft ? 0x0002u : 0x0004u;
        if (right != wantsRight) flags |= wantsRight ? 0x0008u : 0x0010u;
        if (flags != 0 && inject(flags, position))
        {
            lastPosition = position;
            left = wantsLeft;
            right = wantsRight;
        }
    }

    public void Dispose() => Release();

    private void Release()
    {
        armed = false;
        lastPosition = null;
        uint flags = (left ? 0x0004u : 0u) | (right ? 0x0010u : 0u);
        if (flags != 0 && inject(flags, default)) left = right = false;
    }

    private static bool Send(uint flags, Point position)
    {
        var input = new NativeInput { Mouse = new MouseInput { X = position.X, Y = position.Y, Flags = flags } };
        return SendInput(1, in input, InputSize) == 1;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeInput
    {
        public uint Type;
        public MouseInput Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int X, Y;
        public uint Data, Flags, Time;
        public UIntPtr ExtraInfo;
    }

    [DllImport("user32.dll", ExactSpelling = true)]
    private static extern uint SendInput(uint count, in NativeInput inputs, int inputSize);
}
