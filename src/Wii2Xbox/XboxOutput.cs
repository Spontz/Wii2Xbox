using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Exceptions;

namespace Wii2Xbox;

public readonly record struct XboxInput(ushort Buttons, short X, short Y, byte LeftTrigger, byte RightTrigger);

internal interface IXboxOutput : IDisposable
{
    int? Player { get; }
    void Submit(XboxInput input);
}

internal readonly record struct XboxDriverStatus(bool Ready, string Message, bool CanInstall = false);

internal sealed class XboxOutput : IXboxOutput
{
    // Opening the SDK connection verifies the bus without creating an Xbox target.
    public static XboxDriverStatus CheckDriver(Func<IDisposable>? connect = null)
    {
        try
        {
            using var bus = connect is null ? new ViGEmClient() : connect();
            return new(true, UiText.Get("XboxDriverReady"));
        }
        catch (Exception error)
        {
            return new(false, DescribeDriverFailure(error),
                error is VigemBusNotFoundException or VigemBusVersionMismatchException);
        }
    }

    internal static string DescribeDriverFailure(Exception error) => error switch
    {
        VigemBusNotFoundException => UiText.Get("XboxDriverMissing"),
        VigemBusVersionMismatchException => UiText.Get("XboxDriverUnsupported"),
        VigemBusAccessFailedException or UnauthorizedAccessException => UiText.Get("XboxAccessDenied"),
        VigemNoFreeSlotException => UiText.Get("XboxNoSlots"),
        _ => UiText.Format("XboxError", error.Message)
    };

    private readonly ViGEmClient client;
    private readonly IXbox360Controller controller;

    public XboxOutput(Action<bool>? setRumble = null)
    {
        client = new ViGEmClient();
        try
        {
            controller = client.CreateXbox360Controller();
            controller.AutoSubmitReport = false;
            if (setRumble is not null)
                controller.FeedbackReceived += (_, feedback) => setRumble(feedback.LargeMotor != 0 || feedback.SmallMotor != 0);
            controller.Connect();
        }
        catch
        {
            client.Dispose();
            throw;
        }
    }

    public int? Player
    {
        get
        {
            try { return controller.UserIndex + 1; }
            catch (Nefarius.ViGEm.Client.Targets.Xbox360.Exceptions.Xbox360UserIndexNotReportedException)
            { return null; }
        }
    }

    public void Submit(XboxInput input)
    {
        controller.ButtonState = input.Buttons;
        controller.LeftThumbX = input.X;
        controller.LeftThumbY = input.Y;
        controller.LeftTrigger = input.LeftTrigger;
        controller.RightTrigger = input.RightTrigger;
        controller.SubmitReport();
    }

    public void Dispose()
    {
        try { controller.Disconnect(); }
        finally { client.Dispose(); }
    }
}
