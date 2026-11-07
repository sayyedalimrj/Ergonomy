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
                Diagnostics.Record("self-test", "PASS: catalog, settings, startup");
                Shutdown(0);
                return;
            }

            _singleInstance = new Mutex(true, @"Local\Ergonomy.GlassDock.V2", out var first);
            if (!first) { Shutdown(0); return; }

            var dock = new MainWindow();
            MainWindow = dock;
            dock.Show();
            Diagnostics.Record("startup", "Glass Dock created successfully.");
        }
        catch (Exception ex)
        {
            Diagnostics.Record("startup-failure", ex);
            MessageBox.Show("Ergonomy could not start. A detailed log was written to:\n" + Diagnostics.LogPath +
                "\n\n" + ex.Message, "Ergonomy startup", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    private void OnDispatcherException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Diagnostics.Record("ui-failure", e.Exception);
        e.Handled = true;
        try
        {
            MessageBox.Show("A problem occurred in Ergonomy. Diagnostic log:\n" +
                Diagnostics.LogPath + "\n\n" + e.Exception.Message,
                "Ergonomy", MessageBoxButton.OK, MessageBoxImage.Error);
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
    public static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Ergonomy", "logs", "app.log");

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
