using Xunit;

namespace Wii2Xbox.Tests;

public sealed class RecoveryTests
{
    [Theory]
    [InlineData(1, 0x10)]
    [InlineData(2, 0x20)]
    [InlineData(3, 0x40)]
    [InlineData(4, 0x80)]
    public async Task LightsOnlyTheLedOfTheAssignedXboxSlot(int slot, byte mask)
    {
        var transport = new FrameSequence([[0x30, 0, 1], [0x30, 0, 0]]);
        var output = new RecordingOutput(slot);
        var session = new ControllerSession("prueba", () => transport, () => output, new CoreInputMap(new Profile()));
        await session.Completion.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(slot, session.Player);
        var command = Assert.Single(transport.Commands, command => command >= WiiCommand.PlayerLed1);
        var report = new byte[22];
        WiiDevice.EncodeCommand(report, command);
        Assert.Equal(new byte[] { 0x11, mask }, report[..2]);
        Assert.Equal(new ushort[] { 0x1000, 0, 0 }, output.Buttons);
    }

    [Fact]
    public async Task ButtonPreviewReadsPhysicalInputWithoutAnXboxOutput()
    {
        var waiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var released = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var transport = new ScriptedTransport(() =>
        {
            waiting.TrySetResult();
            released.Task.GetAwaiter().GetResult();
            return 0;
        }, () => released.TrySetResult());
        var session = new ControllerSession("prueba", () => transport,
            () => throw new InvalidOperationException("El visor no debe abrir una salida Xbox."),
            new CoreInputMap(new Profile()), mode: SessionMode.Buttons);
        try
        {
            await waiting.Task.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.Equal("Activo", session.Status);
            Assert.Null(session.Player);
            var counts = new int[WiiButtonState.BitCount];
            session.Buttons.Accumulate(long.MaxValue, counts);
            Assert.Equal(1, counts[System.Numerics.BitOperations.TrailingZeroCount((uint)WiiButtons.Two)]);
        }
        finally
        {
            session.Stop();
            await session.Completion.WaitAsync(TimeSpan.FromSeconds(3));
        }
    }

    [Fact]
    public async Task LatencyCaptureCanBeToggledWithoutRestartingTheXboxOutput()
    {
        using var requests = new SemaphoreSlim(0);
        using var responses = new System.Collections.Concurrent.BlockingCollection<int>();
        var timeout = TimeSpan.FromSeconds(3);
        var output = new RecordingOutput();
        var transport = new ScriptedTransport(() =>
        {
            requests.Release();
            return responses.Take();
        }, responses.CompleteAdding);
        var session = Start(transport, output);
        try
        {
            Assert.True(await requests.WaitAsync(timeout));
            Assert.Null(session.Metrics);

            session.SetLatencyCapture(true);
            var capture = Assert.IsType<LatencyMetrics>(session.Metrics);
            responses.Add(3);
            Assert.True(await requests.WaitAsync(timeout));
            Assert.Equal(1, capture.Snapshot().SampleCount);

            session.SetLatencyCapture(false);
            responses.Add(3);
            Assert.True(await requests.WaitAsync(timeout));
            Assert.Same(capture, session.Metrics);
            Assert.Equal(1, capture.Snapshot().SampleCount);
            Assert.False(session.Completion.IsCompleted);

            responses.Add(0);
            await session.Completion.WaitAsync(timeout);
            Assert.Equal(new ushort[] { 0x1000, 0x1000, 0x1000, 0 }, output.Buttons);
        }
        finally
        {
            session.Stop();
            await session.Completion.WaitAsync(timeout);
        }
    }

    [Fact]
    public async Task StopUnblocksReaderAndNeutralizesBeforeDisposal()
    {
        var unblocked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readerWaiting = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var transport = new ScriptedTransport(
            () =>
            {
                readerWaiting.TrySetResult();
                unblocked.Task.GetAwaiter().GetResult();
                throw new ObjectDisposedException("transport");
            },
            () => unblocked.TrySetResult());
        var output = new RecordingOutput();
        var session = Start(transport, output);
        try
        {
            await readerWaiting.Task.WaitAsync(TimeSpan.FromSeconds(3));
            session.Stop();
            await session.Completion.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.False(session.ReconnectRecommended);
            Assert.Equal("Detenido", session.Status);
            Assert.Equal(new ushort[] { 0x1000, 0 }, output.Buttons);
            Assert.True(output.DisconnectedWhileNeutral);
        }
        finally { session.Stop(); }
    }

    [Fact]
    public async Task LostInputRequestsRecoveryAndNeutralizesItsOutput()
    {
        var output = new RecordingOutput();
        var session = Start(new ScriptedTransport(() => throw new TimeoutException()), output);
        await session.Completion.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.True(session.ReconnectRecommended);
        Assert.Equal(new ushort[] { 0x1000, 0 }, output.Buttons);
        Assert.True(output.DisconnectedWhileNeutral);
    }

    [Fact]
    public async Task ASlowHidCloseDoesNotDelayTheNeutralXboxReport()
    {
        var closeEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var allowClose = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var transport = new ScriptedTransport(
            () => throw new IOException("Bluetooth desconectado."),
            () =>
            {
                closeEntered.TrySetResult();
                allowClose.Task.GetAwaiter().GetResult();
            });
        var output = new RecordingOutput();
        var session = Start(transport, output);
        try
        {
            await closeEntered.Task.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.True(output.DisconnectedWhileNeutral);
            Assert.Equal(new ushort[] { 0x1000, 0 }, output.Buttons);
            Assert.False(session.Completion.IsCompleted);
        }
        finally
        {
            allowClose.TrySetResult();
            await session.Completion.WaitAsync(TimeSpan.FromSeconds(3));
        }
        Assert.True(session.ReconnectRecommended);
    }

    [Fact]
    public async Task OutputFailureDoesNotTriggerBluetoothReconnect()
    {
        var session = new ControllerSession("prueba",
            () => new ScriptedTransport(() => throw new InvalidOperationException()),
            () => throw new IOException("Error de salida"),
            new CoreInputMap(new Profile()));
        await session.Completion.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.False(session.ReconnectRecommended);
        Assert.Equal("Error de salida", session.Status);
    }

    [Fact]
    public void RecoveryIsBoundedAndReschedulesAfterSuspend()
    {
        var recovery = new SessionRecovery(SessionMode.XboxAndMouse);
        Assert.False(recovery.HasRetry(100, lostConnection: false));
        recovery.Suspend(wasRunning: true);
        long now = 100;
        foreach (int delay in new[] { 500, 1000, 2000, 4000 })
        {
            Assert.True(recovery.HasRetry(now, lostConnection: false));
            Assert.False(recovery.TryTakeRetry(now + delay - 1));
            now += delay;
            Assert.True(recovery.TryTakeRetry(now));
            Assert.False(recovery.TryTakeRetry(now));
        }
        Assert.False(recovery.HasRetry(now, lostConnection: true));
        Assert.Equal(SessionMode.XboxAndMouse, recovery.Mode);
    }

    [Fact]
    public async Task MalformedAndUnknownReportsAreCountedWithoutInterruptingValidTransitions()
    {
        byte[][] input =
        [
            [0x30, 0, 1],
            [0x30, 0],
            [0x55, 0, 0],
            [0x22, 0, 0, 0x16, 0],
            [0x21, 0, 0, 0x50, 0, 0xfa, 0],
            [0x30, 0, 0]
        ];
        var output = new RecordingOutput();
        var session = new ControllerSession("prueba", () => new FrameSequence(input), () => output,
            new CoreInputMap(new Profile()));
        await session.Completion.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(2, session.Reports);
        Assert.Equal(2, session.TruncatedReports);
        Assert.Equal(1, session.UnknownReports);
        Assert.Equal(new ushort[] { 0x1000, 0, 0 }, output.Buttons);
        Assert.True(output.DisconnectedWhileNeutral);
    }

    [Fact]
    public async Task FourReadersKeepAllOneHundredThousandTransitionsOnTheirOwnOutputs()
    {
        const int framesPerController = 25_000;
        string[] targets = ["A", "B", "X", "Y"];
        var outputs = new RecordingOutput[targets.Length];
        var sessions = new ControllerSession[targets.Length];
        for (int i = 0; i < targets.Length; i++)
        {
            var profile = new Profile();
            profile.Bindings["Two"] = targets[i];
            var transport = new TogglingTransport(framesPerController);
            var output = outputs[i] = new RecordingOutput();
            sessions[i] = new ControllerSession("prueba", () => transport, () => output, new CoreInputMap(profile));
        }
        await Task.WhenAll(sessions.Select(session => session.Completion)).WaitAsync(TimeSpan.FromSeconds(10));
        for (int i = 0; i < outputs.Length; i++)
        {
            var actual = outputs[i].Buttons;
            Assert.Equal(framesPerController + 1, actual.Count);
            ushort expected = Profile.XboxButtons[targets[i]];
            for (int frame = 0; frame < framesPerController; frame++)
                Assert.Equal(frame % 2 == 0 ? expected : (ushort)0, actual[frame]);
            Assert.True(outputs[i].DisconnectedWhileNeutral);
        }
    }

    private sealed class FrameSequence(byte[][] reports) : IControllerTransport
    {
        public List<WiiCommand> Commands { get; } = [];
        private int next;
        public int InputLength => 22;
        public int ReadTimeout { get; set; }
        public int Read(byte[] buffer)
        {
            if (next == reports.Length) return 0;
            byte[] report = reports[next++];
            report.CopyTo(buffer, 0);
            return report.Length;
        }
        public void Send(WiiCommand command, bool infrared) => Commands.Add(command);
        public void Send(InfraredCommand command) { }
        public void Dispose() { }
    }

    [Fact]
    public void InvalidFramesDoNotExtendTheDeadline()
    {
        Assert.Equal(50, InputDeadline.RemainingMs(TimeSpan.FromMilliseconds(800)));
        Assert.Equal(1, InputDeadline.RemainingMs(TimeSpan.FromMilliseconds(849.8)));
        Assert.Throws<TimeoutException>(() => InputDeadline.RemainingMs(TimeSpan.FromMilliseconds(850)));
    }

    private sealed class TogglingTransport(int frameCount) : IControllerTransport
    {
        private int index;
        public int InputLength => 22;
        public int ReadTimeout { get; set; }
        public int Read(byte[] buffer)
        {
            if (index == frameCount) return 0;
            buffer[0] = 0x30;
            buffer[2] = index++ % 2 == 0 ? (byte)1 : (byte)0;
            return 3;
        }
        public void Send(WiiCommand command, bool infrared) { }
        public void Send(InfraredCommand command) { }
        public void Dispose() { }
    }

    private static ControllerSession Start(ScriptedTransport transport, RecordingOutput output) =>
        new("prueba", () => transport, () => output, new CoreInputMap(new Profile()));

    private sealed class ScriptedTransport(Func<int> afterFirst, Action? onClose = null) : IControllerTransport
    {
        private bool first = true;
        public int InputLength => 22;
        public int ReadTimeout { get; set; }
        public int Read(byte[] buffer)
        {
            if (!first) return afterFirst();
            first = false;
            buffer[0] = 0x30;
            buffer[2] = 1;
            return 3;
        }
        public void Send(WiiCommand command, bool infrared) { }
        public void Send(InfraredCommand command) { }
        public void Dispose() => onClose?.Invoke();
    }

    private sealed class RecordingOutput(int player = 1) : IXboxOutput
    {
        public int? Player => player;
        public List<ushort> Buttons { get; } = [];
        public bool DisconnectedWhileNeutral { get; private set; }
        public void Submit(XboxInput state) => Buttons.Add(state.Buttons);
        public void Dispose() => DisconnectedWhileNeutral = Buttons[^1] == 0;
    }
}
