namespace Wii2Xbox;

internal sealed class InfraredActivationDialog : Form
{
    private readonly System.Windows.Forms.Timer timer = new() { Interval = 50 };

    public InfraredActivationDialog(Func<bool> cameraReady)
    {
        Text = UiText.Get("IrActivationTitle");
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;
        AutoScaleMode = AutoScaleMode.Dpi;
        var instructions = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(420, 0),
            Text = UiText.Get("IrActivationInstructions")
        };
        var content = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(16), WrapContents = false };
        content.Controls.Add(instructions);
        Controls.Add(content);

        Shown += (_, _) =>
        {
            long deadline = Environment.TickCount64 + 12000;
            bool timedOut = false;
            timer.Tick += (_, _) =>
            {
                if (cameraReady())
                {
                    timer.Stop();
                    DialogResult = DialogResult.OK;
                }
                else if (!timedOut && Environment.TickCount64 >= deadline)
                {
                    timedOut = true;
                    instructions.Text = UiText.Get("IrActivationTimedOut");
                }
            };
            timer.Start();
        };
    }

    protected override bool ProcessDialogKey(Keys keyData)
    {
        if (keyData != Keys.Escape) return base.ProcessDialogKey(keyData);
        DialogResult = DialogResult.Cancel;
        return true;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) timer.Dispose();
        base.Dispose(disposing);
    }
}
