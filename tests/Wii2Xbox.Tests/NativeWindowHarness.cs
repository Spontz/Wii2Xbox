using System.Drawing;
using System.Windows.Forms;

namespace Wii2Xbox.Tests;

internal sealed class NativeWindowHarness
{
    private static readonly TimeSpan MessageTimeout = TimeSpan.FromSeconds(8);
    private readonly MainForm form;
    private readonly Task finished;

    private NativeWindowHarness(MainForm form, Task finished)
    {
        this.form = form;
        this.finished = finished;
    }

    public static async Task<NativeWindowHarness> Open(List<ControllerSession> sessions)
    {
        var shown = new TaskCompletionSource<MainForm>(TaskCreationOptions.RunContinuationsAsynchronously);
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            try
            {
                // Exercise a real message loop without putting the test window
                // on the user's taskbar or drawing over the working desktop.
                using var window = new MainForm(static () => [])
                {
                    ShowInTaskbar = false,
                    StartPosition = FormStartPosition.Manual,
                    Location = new Point(-20_000, -20_000),
                    Opacity = 0
                };
                for (int i = 0; i < sessions.Count; i++) window.TrackSession($"native-ui-{i}", sessions[i]);
                window.Shown += (_, _) => shown.TrySetResult(window);
                Application.Run(window);
                closed.TrySetResult();
            }
            catch (Exception error)
            {
                shown.TrySetException(error);
                closed.TrySetException(error);
            }
        }) { IsBackground = true, Name = "Wii2Xbox native UI test" };
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return new NativeWindowHarness(await shown.Task.WaitAsync(MessageTimeout), closed.Task);
    }

    public Task<FormWindowState> Interact(int iteration) => Invoke(() =>
    {
        bool minimize = iteration % 4 == 2;
        form.WindowState = minimize ? FormWindowState.Minimized : FormWindowState.Normal;
        if (!minimize)
        {
            form.ClientSize = new Size(iteration % 2 == 0 ? 850 : 930, 410);
            var list = form.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<ListView>().Single();
            var row = list.Items[$"native-ui-{iteration % 4}"]!;
            row.Selected = true;
            row.EnsureVisible();
        }
        return form.WindowState;
    });

    public Task<bool> DetectedReadyDriver() => Invoke(() => form.XboxReady);

    public async Task Close()
    {
        if (!finished.IsCompleted)
            await Invoke(() => { form.Close(); return true; });
        await finished.WaitAsync(MessageTimeout);
    }

    private async Task<T> Invoke<T>(Func<T> action)
    {
        var result = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        form.BeginInvoke(new Action(() =>
        {
            try { result.TrySetResult(action()); }
            catch (Exception error) { result.TrySetException(error); }
        }));
        return await result.Task.WaitAsync(MessageTimeout);
    }
}
