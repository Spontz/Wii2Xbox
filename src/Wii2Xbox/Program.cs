namespace Wii2Xbox;
internal static class Program
{
    [STAThread] static void Main(string[] args)
    {
        bool background = args.Contains(StartupRegistration.BackgroundArgument, StringComparer.OrdinalIgnoreCase);
        using var reveal = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\Wii2Xbox.ShowWindow");
        using var instance = new Mutex(true, @"Local\Wii2Xbox.Instance", out bool first);
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
            using var form = new MainForm();
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
