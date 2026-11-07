using System.ComponentModel;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Win32;
using WinForms = System.Windows.Forms;
using MessageBox = System.Windows.MessageBox;

namespace Ergonomy;

public partial class MainWindow : Window
{
    private AppSettings _settings;
    private readonly ReminderEngine _engine;
    private readonly WinForms.NotifyIcon _tray;
    private ReminderWindow? _overlay;
    private bool _exiting;

    public MainWindow()
    {
        InitializeComponent();
        _settings = SettingsStore.Load();
        LoadFields();
        _engine = new ReminderEngine(_settings);
        _engine.ReminderDue += ShowReminder;
        _engine.Start();

        _tray = new WinForms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Information,
            Text = "Ergonomy — Workplace Wellness",
            Visible = true
        };
        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add("بازکردن Ergonomy", null, (_, _) => Dispatcher.Invoke(ShowDashboard));
        menu.Items.Add("نمایش اعلان آزمایشی", null, (_, _) => Dispatcher.Invoke(() => ShowReminder(ReminderKind.Posture)));
        menu.Items.Add("خروج", null, (_, _) => Dispatcher.Invoke(ExitApp));
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => Dispatcher.Invoke(ShowDashboard);
    }

    private void ShowDashboard()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void LoadFields()
    {
        EyeEnabled.IsChecked = _settings.EyeEnabled;
        PostureEnabled.IsChecked = _settings.PostureEnabled;
        MoveEnabled.IsChecked = _settings.MoveEnabled;
        WaterEnabled.IsChecked = _settings.WaterEnabled;
        EyeMinutes.Text = _settings.EyeMinutes.ToString();
        PostureMinutes.Text = _settings.PostureMinutes.ToString();
        MoveMinutes.Text = _settings.MoveMinutes.ToString();
        WaterMinutes.Text = _settings.WaterMinutes.ToString();
        AutoStart.IsChecked = _settings.AutoStart;
    }

    private static int ParseMinutes(System.Windows.Controls.TextBox field)
    {
        var value = field.Text.Trim().Replace('۰','0').Replace('۱','1').Replace('۲','2')
            .Replace('۳','3').Replace('۴','4').Replace('۵','5').Replace('۶','6')
            .Replace('۷','7').Replace('۸','8').Replace('۹','9');
        if (!int.TryParse(value, out int minutes) || minutes < 1 || minutes > 480)
            throw new ArgumentException("فواصل زمانی باید عددی بین ۱ تا ۴۸۰ دقیقه باشند.");
        return minutes;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var next = new AppSettings
            {
                EyeEnabled = EyeEnabled.IsChecked == true,
                PostureEnabled = PostureEnabled.IsChecked == true,
                MoveEnabled = MoveEnabled.IsChecked == true,
                WaterEnabled = WaterEnabled.IsChecked == true,
                EyeMinutes = ParseMinutes(EyeMinutes),
                PostureMinutes = ParseMinutes(PostureMinutes),
                MoveMinutes = ParseMinutes(MoveMinutes),
                WaterMinutes = ParseMinutes(WaterMinutes),
                AutoStart = AutoStart.IsChecked == true
            };
            SettingsStore.Save(next);
            SettingsStore.ConfigureStartup(next.AutoStart);
            _settings = next;
            _engine.Configure(next);
            StatusText.Text = "تنظیمات ذخیره شد و از همین حالا اعمال می‌شود.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "تنظیمات Ergonomy", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Preview_Click(object sender, RoutedEventArgs e) => ShowReminder(ReminderKind.Posture);

    private void ShowReminder(ReminderKind kind)
    {
        if (_overlay != null) return;
        _overlay = new ReminderWindow(kind);
        _overlay.ActionTaken += response =>
        {
            _engine.Respond(kind, response);
            _overlay = null;
        };
        _overlay.Show();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_exiting)
        {
            e.Cancel = true;
            Hide();
            _tray.ShowBalloonTip(2500, "Ergonomy فعال است", "یادآورها در پس‌زمینه ادامه دارند.", WinForms.ToolTipIcon.Info);
        }
        base.OnClosing(e);
    }

    private void ExitApp()
    {
        _exiting = true;
        _engine.Dispose();
        _overlay?.Close();
        _tray.Visible = false;
        _tray.Dispose();
        System.Windows.Application.Current.Shutdown();
    }
}
