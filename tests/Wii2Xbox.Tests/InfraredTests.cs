using Xunit;

namespace Wii2Xbox.Tests;

public sealed class InfraredTests
{
    [Fact]
    public void ParsesBothBasicModePairsAndRejectsMissingLed()
    {
        var frame = MakeFrame(400, 383, 623, 383);
        Assert.True(InfraredPointer.TryPosition(frame, out var center));
        Assert.InRange(center.X, 32600, 32900);
        Assert.InRange(center.Y, 32600, 32900);
        frame[5] |= 0xc0; // Invalid first LED: Y becomes greater than 767.
        Assert.False(InfraredPointer.TryPosition(frame, out _));
        Assert.False(InfraredPointer.TryPosition(frame.AsSpan(0, 12), out _));
    }

    [Fact]
    public void ClicksReleaseOnLostTrackingAndStop()
    {
        var events = new List<uint>();
        using var mouse = new InfraredMouse((flags, _) => { events.Add(flags); return true; });
        var frame = MakeFrame(400, 383, 623, 383);
        mouse.Update(frame, WiiButtons.A);    // A was held before acquiring the bar.
        mouse.Update(frame, WiiButtons.None);
        mouse.Update(frame, WiiButtons.A);
        mouse.Update([], WiiButtons.A);
        mouse.Update(frame, WiiButtons.A);    // No click while A is still held.
        mouse.Update(frame, WiiButtons.None);
        mouse.Update(frame, WiiButtons.B);
        mouse.Dispose();
        Assert.Equal(new uint[] { 0x8001, 2, 4, 0x8001, 8, 16 }, events);
    }

    [Fact]
    public void CalibrationMapsBothTargetsAndAllowsInvertedAxes()
    {
        var first = new System.Drawing.Point(200, 650);
        var second = new System.Drawing.Point(800, 150);
        var calibration = InfraredCalibration.FromTargets(first, second);
        Assert.NotNull(calibration);
        var upper = calibration.Map(first);
        var lower = calibration.Map(second);
        Assert.InRange(upper.X, 5460, 5462);
        Assert.InRange(upper.Y, 5460, 5462);
        Assert.InRange(lower.X, 60073, 60075);
        Assert.InRange(lower.Y, 60073, 60075);
        Assert.Equal(new System.Drawing.Point(0, 65535), calibration.Map(new(0, 0)));
        Assert.Null(InfraredCalibration.FromTargets(first, new(210, 640)));
    }

    [Fact]
    public void CalibrationSuppressesPointerAndConsumesOnlyAButtonEdgesWithTracking()
    {
        var events = new List<uint>();
        using var mouse = new InfraredMouse((flags, _) => { events.Add(flags); return true; });
        var capture = new InfraredCalibrationCapture();
        var frame = MakeFrame(400, 383, 623, 383);
        mouse.Update(frame, WiiButtons.None);
        mouse.Update(frame, WiiButtons.A);
        mouse.Update(frame, WiiButtons.A, capture: capture);
        Assert.Null(capture.Take());
        mouse.Update(frame, WiiButtons.None, capture: capture);
        mouse.Update([], WiiButtons.A, capture: capture);
        mouse.Update(frame, WiiButtons.A, capture: capture);
        Assert.Null(capture.Take());
        mouse.Update(frame, WiiButtons.None, capture: capture);
        mouse.Update(frame, WiiButtons.A, capture: capture);
        Assert.Equal(new System.Drawing.Point(512, 384), capture.Take());
        mouse.Update(frame, WiiButtons.A, capture: capture);
        Assert.Null(capture.Take());
        Assert.Equal(new uint[] { 0x8001, 2, 4 }, events);
    }

    [Fact]
    public void IrSetupWaitsForAcknowledgementsAndSelectsCombinedReport()
    {
        var setup = new InfraredSetup(0);
        Assert.Null(setup.Process([0x30, 0, 0], 0));
        Assert.Equal(InfraredCommand.Camera, setup.Process([0x22, 0, 0, 0x13, 0], 0));
        Assert.Equal(InfraredCommand.Enable, setup.Process([0x22, 0, 0, 0x1a, 0], 0));
        Assert.Equal(InfraredCommand.Sensitivity1, setup.Process([0x22, 0, 0, 0x16, 0], 0));
        Assert.Equal(InfraredCommand.Sensitivity2, setup.Process([0x22, 0, 0, 0x16, 0], 0));
        Assert.Equal(InfraredCommand.BasicMode, setup.Process([0x22, 0, 0, 0x16, 0], 0));
        Assert.Equal(InfraredCommand.Finish, setup.Process([0x22, 0, 0, 0x16, 0], 0));
        Assert.Null(setup.Process([0x22, 0, 0, 0x16, 0], 0));
        Assert.True(setup.Enabled);
        Span<byte> report = stackalloc byte[22];
        WiiDevice.EncodeCommand(report, WiiCommand.ExtensionMode, infrared: true);
        Assert.Equal((byte)0x36, report[2]);
        InfraredSetup.Encode(report, InfraredCommand.BasicMode);
        Assert.Equal(new byte[] { 0x16, 0x04, 0xb0, 0, 0x33, 1, 1 }, report[..7].ToArray());
    }

    [Fact]
    public async Task CombinedSessionRoutesTheSameReportToXboxNunchukAndMouse()
    {
        var transport = new InfraredTransport(expectExtension: true);
        var events = new List<uint>();
        var xbox = new OutputTrace();
        var session = new ControllerSession("Wii IR", () => transport, () => xbox, new CoreInputMap(new Profile()),
            mode: SessionMode.XboxAndMouse, openMouse: () => new InfraredMouse((flags, _) => { events.Add(flags); return true; }));
        await session.Completion.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(Enum.GetValues<InfraredCommand>(), transport.Initialization);
        Assert.Contains((WiiCommand.ExtensionMode, true), transport.Commands);
        Assert.Contains(xbox.States, state => (state.Buttons & 0x4000) != 0 &&
            state.X == short.MaxValue && state.Y == short.MinValue && state.LeftTrigger == 255);
        Assert.Equal(new uint[] { 0x8001, 2, 4 }, events);
        Assert.Equal(default, xbox.States[^1]);
        Assert.True(xbox.Disposed);
        Assert.True(transport.Disposed);
    }

    [Fact]
    public async Task MouseOnlyUsesInfraredWithoutOpeningXboxOrConfiguringNunchuk()
    {
        var transport = new InfraredTransport(expectExtension: false);
        var events = new List<uint>();
        int xboxRequests = 0;
        var session = new ControllerSession("Wii IR", () => transport,
            () => { xboxRequests++; throw new InvalidOperationException("Xbox no debe abrirse"); },
            new CoreInputMap(new Profile()), collectLatency: true, mode: SessionMode.Mouse,
            openMouse: () => new InfraredMouse((flags, _) => { events.Add(flags); return true; }));
        await session.Completion.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(0, xboxRequests);
        Assert.Null(session.Metrics);
        Assert.DoesNotContain(transport.Commands, command => command.Item1 is WiiCommand.Init or WiiCommand.Identify);
        Assert.Equal(new uint[] { 0x8001, 2, 4 }, events);
        Assert.True(transport.Disposed);
    }

    [Fact]
    public void RejectedIrCommandLeavesCameraModeDisabled()
    {
        var setup = new InfraredSetup(0);
        Assert.Null(setup.Process([0x22, 0, 0, 0x13, 3], 0));
        Assert.False(setup.Pending);
        Assert.False(setup.Enabled);
    }

    private static byte[] MakeFrame(int x1, int y1, int x2, int y2)
    {
        var frame = new byte[22];
        frame[0] = 0x36;
        frame[3] = (byte)x1;
        frame[4] = (byte)y1;
        frame[5] = (byte)(((x1 >> 8) << 4) | ((y1 >> 8) << 6) | (x2 >> 8) | ((y2 >> 8) << 2));
        frame[6] = (byte)x2;
        frame[7] = (byte)y2;
        Array.Fill(frame, (byte)255, 8, 14);
        return frame;
    }

    private sealed class OutputTrace : IXboxOutput
    {
        public int? Player => 1;
        public List<XboxInput> States { get; } = [];
        public bool Disposed { get; private set; }
        public void Submit(XboxInput input) => States.Add(input);
        public void Dispose() => Disposed = true;
    }

    private sealed class InfraredTransport(bool expectExtension) : IControllerTransport
    {
        private readonly Queue<byte[]> frames = new();
        private bool started;
        public int InputLength => 22;
        public int ReadTimeout { get; set; }
        public bool Disposed { get; private set; }
        public List<InfraredCommand> Initialization { get; } = [];
        public List<(WiiCommand, bool)> Commands { get; } = [];

        public int Read(byte[] buffer)
        {
            if (!frames.TryDequeue(out var next)) return 0;
            next.CopyTo(buffer, 0);
            return next.Length;
        }

        public void Send(InfraredCommand command)
        {
            Initialization.Add(command);
            byte id = command switch { InfraredCommand.Clock => 0x13, InfraredCommand.Camera => 0x1a, _ => 0x16 };
            frames.Enqueue([0x22, 0, 0, id, 0]);
        }

        public void Send(WiiCommand command, bool infrared)
        {
            Commands.Add((command, infrared));
            switch (command)
            {
                case WiiCommand.Status:
                    frames.Enqueue([0x20, 0, 0, 2, 0, 0, 0]); // Nunchuk connected.
                    break;
                case WiiCommand.CoreMode when infrared && !started:
                    started = true;
                    break;
                case WiiCommand.CoreMode when infrared && frames.Count == 0:
                    if (expectExtension) frames.Enqueue(MakeFrame(400, 383, 623, 383));
                    else EnqueueInput();
                    break;
                case WiiCommand.Init:
                case WiiCommand.DisableEncryption:
                    frames.Enqueue([0x22, 0, 0, 0x16, 0]);
                    break;
                case WiiCommand.Identify:
                    frames.Enqueue([0x21, 0, 0, 0x50, 0, 0xfa, 0, 0, 0xa4, 0x20, 0, 0]);
                    break;
                case WiiCommand.ReadCalibration:
                    frames.Enqueue([0x21, 0, 0, 0xf0, 0, 0x20, 0, 0, 0, 0, 0, 0, 0, 0, 225, 40, 130, 204, 50, 120, 0, 0]);
                    break;
                case WiiCommand.ExtensionMode:
                    EnqueueInput();
                    break;
            }
        }

        public void Dispose() => Disposed = true;

        private void EnqueueInput()
        {
            frames.Enqueue(MakeFrame(400, 383, 623, 383));
            var held = MakeFrame(400, 383, 623, 383);
            held[2] = 8;
            held[13] = 225;
            held[14] = 50;
            held[18] = 0;
            frames.Enqueue(held);
            var lost = (byte[])held.Clone();
            Array.Fill(lost, (byte)255, 3, 10);
            frames.Enqueue(lost);
        }
    }
}
