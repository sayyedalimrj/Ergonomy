using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;

namespace Ergonomy;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings;
    private readonly ReminderEngine _engine;
    private readonly DispatcherTimer _uiTick;
    private readonly DispatcherTimer _eyeTimer;
    private WinForms.NotifyIcon? _tray;
    private bool _expanded;
    private bool _closing;
    private bool _ready;
    private ReminderKind? _activeAlert;
    private int _eyeSeconds;

    private const double CompactHeight = 92;

    public MainWindow()
    {
        InitializeComponent();
        _settings = SettingsStore.Load();
        _engine = new ReminderEngine(_settings);
        _engine.ReminderDue += kind => Dispatcher.Invoke(() => ShowCoach(kind));
        _eyeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _eyeTimer.Tick += EyeTick;
        _uiTick = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _uiTick.Tick += (_, _) => UpdateStatus();

        GuideFilter.SelectedIndex = 0;
        GuidesList.ItemsSource = HseCatalog.All;
        GuideCount.Text = $"{HseCatalog.All.Count} راهنمای کاربردی از منابع معتبر";
        RunOnStartup.IsChecked = _settings.AutoStart;
        HydrationToggle.IsChecked = _settings.WaterEnabled;
        _ready = true;

        InitTraySafely();
        SettingsStore.EnsureStartup(_settings.AutoStart);
        _engine.Start();
        _uiTick.Start();
        Loaded += (_, _) =>
        {
            PositionDock();
            UpdateStatus();
            Diagnostics.Record("ui", "Dock loaded and visible.");
        };
        SystemEventsSafeSubscribe();
    }

    private void SystemEventsSafeSubscribe()
    {
        try { Microsoft.Win32.SystemEvents.DisplaySettingsChanged += OnDisplayChanged; }
        catch (Exception ex) { Diagnostics.Record("display-hook", ex); }
    }

    private void OnDisplayChanged(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke((Action)(() => PositionDock()));

    private void PositionDock()
    {
        // WPF WorkArea uses device independent pixels, just like WPF Window.
        var area = SystemParameters.WorkArea;
        Left = area.Left + Math.Max(0, (area.Width - Width) / 2);
        Top = area.Top + 6;
    }

    private void InitTraySafely()
    {
        try
        {
            _tray = new WinForms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                Text = "Ergonomy · Glass Dock",
                Visible = true
            };
            var menu = new WinForms.ContextMenuStrip();
            menu.Items.Add("نمایش پنل", null, (_, _) =>
                Dispatcher.BeginInvoke((Action)(() => { if (!_expanded) SetExpanded(true); })));
            menu.Items.Add("توقف ۱ ساعت", null, (_, _) =>
                Dispatcher.BeginInvoke((Action)(() => { _engine.PauseFor(TimeSpan.FromHours(1)); UpdateStatus(); })));
            menu.Items.Add("ادامه یادآورها", null, (_, _) =>
                Dispatcher.BeginInvoke((Action)(() => { _engine.Resume(); UpdateStatus(); })));
            menu.Items.Add("خروج", null, (_, _) =>
                Dispatcher.BeginInvoke((Action)ExitApp));
            _tray.ContextMenuStrip = menu;
            _tray.DoubleClick += (_, _) => Dispatcher.BeginInvoke((Action)(() => SetExpanded(true)));
        }
        catch (Exception ex)
        {
            Diagnostics.Record("tray-fallback", ex);
            _tray?.Dispose();
            _tray = null; // The dock must still work if tray initialization fails.
        }
    }

    private void UpdateStatus()
    {
        if (!_ready) return;
        if (_engine.IsPaused)
        {
            NextReminderText.Text = "یادآورها موقتاً متوقف‌اند";
            ModeText.Text = "برای ادامه، دکمه ▶ را بزن";
            TodayStatus.Text = "حالت استراحت فعال است";
            PauseButton.Content = "▶";
            return;
        }
        PauseButton.Content = "Ⅱ";
        if (_engine.IsIdle)
        {
            NextReminderText.Text = "زمان‌سنج هنگام بی‌کاری متوقف است";
            ModeText.Text = "با بازگشت به کار ادامه می‌دهد";
            TodayStatus.Text = "شمارنده با فعالیت دوباره ادامه می‌دهد";
            return;
        }
        var next = _engine.NextReminder;
        if (next is null)
        {
            NextReminderText.Text = "یادآوری‌ها خاموش‌اند";
            ModeText.Text = "از منوی تنظیمات فعال‌شان کن";
            return;
        }
        var label = next.Value.Kind switch
        {
            ReminderKind.Eye => "چشم‌ها",
            ReminderKind.Posture => "وضعیت نشستن",
            ReminderKind.Move => "حرکت و استراحت",
            _ => "آب خوردن"
        };
        var minutes = Math.Max(1, (int)Math.Ceiling(next.Value.Remaining.TotalMinutes));
        NextReminderText.Text = $"یادآوری بعدی: {label} · {minutes} دقیقه";
        ModeText.Text = "◉  فعال و خودکار · بدون ردیابی تصویر";
        TodayStatus.Text = "حالت مراقبت خودکار روشن است";
    }

    private double ExpandedHeight =>
        Math.Max(355, Math.Min(704, SystemParameters.WorkArea.Height - 30));

    private void SetExpanded(bool expanded)
    {
        if (_expanded == expanded) return;
        _expanded = expanded;
        if (expanded)
        {
            ExpandedArea.Visibility = Visibility.Visible;
            ExpandedArea.Opacity = 0;
        }
        ExpandButton.Content = expanded ? "⌃" : "⌄";
        BeginAnimation(HeightProperty, null);
        var target = expanded ? ExpandedHeight : CompactHeight;
        var animation = new DoubleAnimation(Height, target, TimeSpan.FromMilliseconds(390))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            FillBehavior = FillBehavior.Stop
        };
        animation.Completed += (_, _) =>
        {
            Height = target;
            if (!expanded && !_expanded) ExpandedArea.Visibility = Visibility.Collapsed;
        };
        BeginAnimation(HeightProperty, animation);
        if (expanded)
        {
            ExpandedArea.BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1,
                TimeSpan.FromMilliseconds(370)) { FillBehavior = FillBehavior.Stop });
            ExpandedArea.Opacity = 1;
        }
    }

    private void Expand_Click(object sender, RoutedEventArgs e) => SetExpanded(!_expanded);
    private void Pause_Click(object sender, RoutedEventArgs e)
    {
        if (_engine.IsPaused) _engine.Resume();
        else
        {
            if (_activeAlert is ReminderKind current) CompleteAlert(current, ReminderResponse.Dismissed);
            _engine.PauseFor(TimeSpan.FromHours(1));
        }
        UpdateStatus();
    }

    private void SelectPage(string name)
    {
        TodayPage.Visibility = name == "today" ? Visibility.Visible : Visibility.Collapsed;
        StandardsPage.Visibility = name == "standards" ? Visibility.Visible : Visibility.Collapsed;
        DeskPage.Visibility = name == "desk" ? Visibility.Visible : Visibility.Collapsed;
        InfoPage.Visibility = name == "info" ? Visibility.Visible : Visibility.Collapsed;
        TodayTab.Background = name == "today" ? "#51517691".ToBrush() : "#1722343F".ToBrush();
        StandardsTab.Background = name == "standards" ? "#51517691".ToBrush() : "#1722343F".ToBrush();
        DeskTab.Background = name == "desk" ? "#51517691".ToBrush() : "#1722343F".ToBrush();
        InfoTab.Background = name == "info" ? "#51517691".ToBrush() : "#1722343F".ToBrush();
    }

    private void Today_Click(object sender, RoutedEventArgs e) => SelectPage("today");
    private void Standards_Click(object sender, RoutedEventArgs e) => SelectPage("standards");
    private void Desk_Click(object sender, RoutedEventArgs e) => SelectPage("desk");
    private void Info_Click(object sender, RoutedEventArgs e) => SelectPage("info");

    private void Filter_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_ready || GuideFilter.SelectedItem is not ComboBoxItem filter) return;
        var group = filter.Content?.ToString() ?? "همهٔ موضوعات";
        var entries = group == "همهٔ موضوعات"
            ? HseCatalog.All.ToList()
            : HseCatalog.All.Where(g => g.Group == group).ToList();
        GuidesList.ItemsSource = entries;
        GuideCount.Text = $"{entries.Count} راهنمای کاربردی از منابع معتبر";
    }

    private void Source_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string url) return;
        try
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Scheme == Uri.UriSchemeHttps)
                Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception ex) { Diagnostics.Record("open-source", ex); }
    }

    private void ShowCoach(ReminderKind kind)
    {
        if (_activeAlert != null) return;
        _activeAlert = kind;
        _eyeTimer.Stop();
        EyeCountdownButton.Visibility = kind == ReminderKind.Eye ? Visibility.Visible : Visibility.Collapsed;
        EyeCountdownButton.IsEnabled = true;
        EyeCountdownButton.Content = "شروع ۲۰ ثانیه";
        (ReminderTitle.Text, ReminderDetails.Text) = kind switch
        {
            ReminderKind.Eye => ("◉  یک فرصت کوتاه برای چشم‌ها",
                "۲۰ ثانیه به چیزی در فاصلهٔ حدود ۶ متر نگاه کن. دکمهٔ شروع، زمان را می‌شمارد؛ انجام را خودت تأیید کن."),
            ReminderKind.Posture => ("◈  وضعیت بدنت را بررسی کن",
                "شانه‌ها را رها کن؛ گردن و مچ‌ها را در وضعیت راحت نگه دار، پاها را حمایت کن و حالت بدنت را کمی تغییر بده."),
            ReminderKind.Move => ("↗  وقت تغییر فعالیت است",
                "اگر شرایط کار اجازه می‌دهد، ۵ تا ۱۰ دقیقه از نمایشگر دور شو، راه برو یا به فعالیت دیگری بپرداز."),
            _ => ("◉  یادآوری آب",
                "اگر نیاز به آب داری، کمی بنوش. این پیشنهاد اختیاری است و مقدار ثابت پزشکی تجویز نمی‌کند.")
        };
        ReminderBanner.Visibility = Visibility.Visible;
        SelectPage("today");
        SetExpanded(true);
        Diagnostics.Record("reminder", kind.ToString()); // only reminder type, no user activity history
    }

    private void CompleteAlert(ReminderKind kind, ReminderResponse response)
    {
        _eyeTimer.Stop();
        _engine.Respond(kind, response);
        _activeAlert = null;
        ReminderBanner.Visibility = Visibility.Collapsed;
        if (response == ReminderResponse.Snooze) FooterText.Text = "یادآورها ۱۰ دقیقه به تعویق افتادند.";
        else FooterText.Text = "✦  یادآورها به‌طور خودکار ادامه خواهند داشت.";
        UpdateStatus();
    }

    private void Done_Click(object sender, RoutedEventArgs e)
    {
        if (_activeAlert is ReminderKind kind) CompleteAlert(kind, ReminderResponse.Done);
    }

    private void Snooze_Click(object sender, RoutedEventArgs e)
    {
        if (_activeAlert is ReminderKind kind) CompleteAlert(kind, ReminderResponse.Snooze);
    }

    private void EyeCountdown_Click(object sender, RoutedEventArgs e)
    {
        _eyeSeconds = 20;
        EyeCountdownButton.IsEnabled = false;
        EyeCountdownButton.Content = "۲۰ ثانیه";
        _eyeTimer.Start();
    }

    private void EyeTick(object? sender, EventArgs e)
    {
        _eyeSeconds--;
        if (_eyeSeconds > 0) EyeCountdownButton.Content = $"{_eyeSeconds} ثانیه";
        else
        {
            _eyeTimer.Stop();
            EyeCountdownButton.Content = "۲۰ ثانیه تمام شد ✓";
            // Time elapsed is not proof the user took a break; keep Done as a separate action.
        }
    }

    private void EyePractice_Click(object sender, RoutedEventArgs e) => ShowPractice(ReminderKind.Eye);
    private void PosturePractice_Click(object sender, RoutedEventArgs e) => ShowPractice(ReminderKind.Posture);
    private void MovePractice_Click(object sender, RoutedEventArgs e) => ShowPractice(ReminderKind.Move);

    private void ShowPractice(ReminderKind kind)
    {
        if (_activeAlert is ReminderKind current) CompleteAlert(current, ReminderResponse.Dismissed);
        ShowCoach(kind);
    }

    private void Checklist_Changed(object sender, RoutedEventArgs e)
    {
        if (!_ready) return;
        var checklist = new[]
        {
            CheckMonitor, CheckTop, CheckBack, CheckFeet,
            CheckHands, CheckLight, CheckCable, CheckMove
        };
        CheckProgressText.Text = $"{checklist.Count(c => c.IsChecked == true)} از ۸ مورد بررسی شده";
    }

    private void ResetChecklist_Click(object sender, RoutedEventArgs e)
    {
        foreach (var check in new[]
        {
            CheckMonitor, CheckTop, CheckBack, CheckFeet,
            CheckHands, CheckLight, CheckCable, CheckMove
        }) check.IsChecked = false;
    }

    private void Preference_Changed(object sender, RoutedEventArgs e)
    {
        if (!_ready) return;
        _settings.AutoStart = RunOnStartup.IsChecked == true;
        _settings.WaterEnabled = HydrationToggle.IsChecked == true;
        try
        {
            SettingsStore.Save(_settings);
            SettingsStore.EnsureStartup(_settings.AutoStart);
        }
        catch (Exception ex) { Diagnostics.Record("save-preferences", ex); }
        _engine.Configure(_settings);
        UpdateStatus();
    }

    private void Exit_Click(object sender, RoutedEventArgs e) => ExitApp();

    private void ExitApp()
    {
        if (_closing) return;
        _closing = true;
        _uiTick.Stop();
        _eyeTimer.Stop();
        _engine.Dispose();
        try { Microsoft.Win32.SystemEvents.DisplaySettingsChanged -= OnDisplayChanged; } catch { }
        if (_tray != null) { _tray.Visible = false; _tray.Dispose(); }
        Application.Current.Shutdown();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_closing)
        {
            e.Cancel = true;
            SetExpanded(false);
            return;
        }
        base.OnClosing(e);
    }
}

internal static class BrushHelpers
{
    public static System.Windows.Media.Brush ToBrush(this string hex) =>
        (System.Windows.Media.Brush)new System.Windows.Media.BrushConverter().ConvertFromString(hex)!;
}
