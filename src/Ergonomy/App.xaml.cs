using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using MessageBox = System.Windows.MessageBox;

namespace Ergonomy;

public partial class App : System.Windows.Application
{
    private Mutex? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if(e.Args.Any(x => x.Contains("self-test", StringComparison.OrdinalIgnoreCase)))
            Environment.SetEnvironmentVariable("ERGONOMY_DATA_ROOT", Path.Combine(Path.GetTempPath(), "Ergonomy-test", Guid.NewGuid().ToString("N")));
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            Diagnostics.Record("unhandled-domain", args.ExceptionObject as Exception);
        DispatcherUnhandledException += OnDispatcherException;
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Diagnostics.Record("unobserved-task", args.Exception);
            args.SetObserved();
        };
        try
        {
            if (e.Args.Any(x => x.Equals("--self-test", StringComparison.OrdinalIgnoreCase)))
            {
                if (HseCatalog.All.Count < 15) throw new InvalidOperationException("Guidance catalog not initialized.");
                var settings = SettingsStore.Load();
                if (settings.EyeMinutes <= 0 || settings.MoveMinutes <= 0)
                    throw new InvalidOperationException("Invalid built-in reminder schedule.");
                settings.EyeMinutes = 27; settings.ReducedMotion = true;
                SettingsStore.Save(settings);
                var loaded = SettingsStore.Load();
                if(loaded.EyeMinutes != 27 || !loaded.ReducedMotion) throw new InvalidOperationException("Settings round trip failed.");
                HistoryStore.Save([new(DateTimeOffset.Now, ReminderKind.Eye, ReminderResponse.Done)]);
                if(HistoryStore.Load().Count != 1) throw new InvalidOperationException("History round trip failed.");
                File.WriteAllText(Path.Combine(LocalData.Root, "settings.json"), "{broken");
                if(SettingsStore.Load().EyeMinutes != 20) throw new InvalidOperationException("Settings corruption fallback failed.");
                Diagnostics.Record("self-test", "PASS: catalog, settings round trip, history round trip, corrupt JSON fallback, startup");
                Shutdown(0);
                return;
            }

            if (e.Args.Any(x => x.Equals("--ui-self-test", StringComparison.OrdinalIgnoreCase)))
            {
                var probe = new MainWindow(diagnosticOnly: true);
                probe.Show();
                probe.RunUiSelfTest();
                Diagnostics.Record("ui-self-test", "PASS: hidden host, physical side-edge pointer reveal, compact layout, embedded Yekan glyphs, catalog rendering, personal reminder CRUD, timer persistence, button filters, workstation self-check, edge cue, icon, Persian About and movement navigation exercised.");
                Shutdown(0);
                return;
            }

            _singleInstance = new Mutex(true, @"Local\Ergonomy.GlassDock.V2", out var first);
            if (!first) { Shutdown(0); return; }

            var dock = new MainWindow();
            MainWindow = dock;
            dock.Show();
            Diagnostics.Record("startup", "Minimal corner host created successfully.");
        }
        catch (Exception ex)
        {
            Diagnostics.Record("startup-failure", ex);
            if (!e.Args.Any(x => x.Contains("self-test", StringComparison.OrdinalIgnoreCase)))
                MessageBox.Show("همیار سلامت اجرا نشد. گزارش خطا در این مسیر است:\n" + Diagnostics.LogPath +
                "\n\n" + ex.Message, "اجرای همیار سلامت", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Diagnostics.Record("ui-failure", e.Exception);
        e.Handled = true;
        try
        {
            MessageBox.Show("خطایی در همیار سلامت رخ داد. مسیر گزارش:\n" +
                Diagnostics.LogPath + "\n\n" + e.Exception.Message,
                "همیار سلامت", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { Shutdown(1); }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try { _singleInstance?.ReleaseMutex(); }
        catch (ApplicationException) { }
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}

public static class Diagnostics
{
    public static readonly string LogPath = Path.Combine(LocalData.Root, "logs", "app.log");

    public static void Record(string tag, Exception? exception) =>
        Record(tag, exception?.ToString() ?? "(no exception details)");

    public static void Record(string tag, string description)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            File.AppendAllText(LogPath,
                DateTimeOffset.Now.ToString("O") + " [" + tag + "] " + description + Environment.NewLine);
        }
        catch { /* Logging must never make the app fail. */ }
    }
}
