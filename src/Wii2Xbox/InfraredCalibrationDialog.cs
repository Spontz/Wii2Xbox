namespace Wii2Xbox;

internal sealed class InfraredCalibrationDialog : Form
{
    private readonly ControllerSession session;
    private readonly InfraredCalibrationCapture capture;
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 50 };
    private Point? first;
    private bool tracking;
    private bool invalid;
    public InfraredCalibration? Result { get; private set; }

    public InfraredCalibrationDialog(ControllerSession session, InfraredCalibrationCapture capture)
    {
        this.session = session;
        this.capture = capture;
        Text = UiText.Get("CalibrateMouseTitle");
        StartPosition = FormStartPosition.Manual;
        FormBorderStyle = FormBorderStyle.None;
        Bounds = Screen.PrimaryScreen?.Bounds ?? Screen.FromPoint(Point.Empty).Bounds;
        ShowInTaskbar = false;
        DoubleBuffered = true;
        KeyPreview = true;
        BackColor = SystemColors.Window;
        ForeColor = SystemColors.WindowText;
        timer.Tick += (_, _) => ReadCapture();
        Shown += (_, _) => timer.Start();
        KeyDown += (_, key) =>
        {
            if (key.KeyCode == Keys.Escape) DialogResult = DialogResult.Cancel;
            if (key.KeyCode == Keys.R) DialogResult = DialogResult.OK; // Null clears the saved calibration.
        };
    }

    private void ReadCapture()
    {
        if (session.Completion.IsCompleted)
        {
            DialogResult = DialogResult.Abort;
            return;
        }
        bool visible = capture.Tracking;
        if (visible != tracking)
        {
            tracking = visible;
            Invalidate();
        }
        if (capture.Take() is not { } point) return;
        if (first is not { } topLeft)
        {
            first = point;
            invalid = false;
        }
        else if (InfraredCalibration.FromTargets(topLeft, point) is { } calibration)
        {
            Result = calibration;
            DialogResult = DialogResult.OK;
            return;
        }
        else
        {
            first = null;
            invalid = true;
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        int x = first is null ? ClientSize.Width / 12 : ClientSize.Width * 11 / 12;
        int y = first is null ? ClientSize.Height / 12 : ClientSize.Height * 11 / 12;
        int radius = Math.Max(12, DeviceDpi / 6);
        var targetColor = SystemInformation.HighContrast
            ? tracking ? SystemColors.Highlight : SystemColors.WindowText
            : Application.IsDarkModeEnabled
                ? tracking ? Color.FromArgb(84, 235, 181) : Color.Gold
                : tracking ? Color.FromArgb(17, 112, 70) : Color.FromArgb(150, 92, 0);
        using var target = new Pen(targetColor, 3);
        e.Graphics.DrawEllipse(target, x - radius, y - radius, radius * 2, radius * 2);
        e.Graphics.DrawLine(target, x - radius * 2, y, x + radius * 2, y);
        e.Graphics.DrawLine(target, x, y - radius * 2, x, y + radius * 2);
        var text = first is null ? UiText.Get("CalibrateFirstTarget") : UiText.Get("CalibrateSecondTarget");
        var detail = invalid ? UiText.Get("CalibrationPointsTooClose")
            : tracking ? UiText.Get("SensorBarDetected") : UiText.Get("SensorBarNotVisible");
        var box = new Rectangle(16, ClientSize.Height / 2 - 70, ClientSize.Width - 32, 140);
        using var instructions = new Font("Segoe UI", 16, FontStyle.Regular);
        TextRenderer.DrawText(e.Graphics, $"{text}\n{detail}\n{UiText.Get("CalibrationShortcuts")}",
            instructions, box, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.WordBreak);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) timer.Dispose();
        base.Dispose(disposing);
    }
}
