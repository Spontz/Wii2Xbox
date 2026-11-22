namespace Wii2Xbox;
internal static class Program
{
    [STAThread] static void Main(string[] args)
    {
        bool background = args.Contains(StartupRegistration.BackgroundArgument, StringComparer.OrdinalIgnoreCase);
        string? updateResult = args.FirstOrDefault(arg => arg.StartsWith(AppUpdater.ResultArgument, StringComparison.Ordinal))
            ?[AppUpdater.ResultArgument.Length..];
        using var updateGate = new Mutex(false, AppUpdater.UpdateMutex);
        bool canStart;
        try { canStart = updateGate.WaitOne(0); }
        catch (AbandonedMutexException) { canStart = true; }
        if (!canStart) return;

        using var reveal = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\Wii2Xbox.ShowWindow");
        Mutex instance;
        bool first;
        try { instance = new Mutex(true, @"Local\Wii2Xbox.Instance", out first); }
        finally { updateGate.ReleaseMutex(); }
        using var instanceHandle = instance;
        if (!first)
        {
            if (!background) reveal.Set();
            return;
        }

        try
        {
            UiLanguage.Initialize();
            ApplicationConfiguration.Initialize();
            Application.SetColorMode(SystemColorMode.System);
            using var context = new ApplicationContext();
            using var updater = AppUpdater.ForCurrentProcess();
            using var form = new MainForm(WiiDevice.Find, updater, updateResult);
            form.FormClosed += (_, _) => context.ExitThread();
            form.StartResident(background);
            var registration = ThreadPool.RegisterWaitForSingleObject(reveal, (_, _) =>
            {
                try { form.BeginInvoke((Action)form.RestoreWindow); }
                catch (InvalidOperationException) { }
            }, null, Timeout.Infinite, executeOnlyOnce: false);
            try { Application.Run(context); }
            finally { registration.Unregister(null); }

        }
        finally { instance.ReleaseMutex(); }
    }
}
