using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Ergonomy;

public partial class ReminderWindow : Window
{
    private readonly DispatcherTimer _autoClose;
    private bool _answered;
    public event Action<ReminderResponse>? ActionTaken;

    public ReminderWindow(ReminderKind kind)
    {
        InitializeComponent();
        (Heading.Text, Description.Text) = kind switch
        {
            ReminderKind.Eye => ("چشم‌ها هم استراحت می‌خواهند 👁",
                "۲۰ ثانیه به نقطه‌ای در فاصلهٔ حدود ۶ متر نگاه کن و چند بار آرام پلک بزن."),
            ReminderKind.Posture => ("وضعیت نشستن را بررسی کن",
                "گردن در راستای بدن، شانه‌ها آزاد، کمر تکیه‌داده و پاها حمایت‌شده باشند. حالتت را تغییر بده."),
            ReminderKind.Move => ("زمان یک وقفهٔ حرکتی رسیده",
                "۵ تا ۱۰ دقیقه از صفحه دور شو؛ کمی راه برو یا فعالیتت را عوض کن."),
            _ => ("آب خوردن یادت نرود",
                "اگر تشنه‌ای، آب بنوش. نیاز به مایعات برای همه یکسان نیست.")
        };
        _autoClose = new DispatcherTimer { Interval = TimeSpan.FromSeconds(20) };
        _autoClose.Tick += (_, _) => Respond(ReminderResponse.AutoDismiss);
        Loaded += (_, _) =>
        {
            Left = SystemParameters.WorkArea.Right - Width - 22;
            Top = SystemParameters.WorkArea.Bottom - Height - 22;
            Opacity = 0;
            var shift = new TranslateTransform(0, 24);
            RenderTransform = shift;
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(400)));
            shift.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(24, 0, TimeSpan.FromMilliseconds(460))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
            _autoClose.Start();
        };
        Closed += (_, _) =>
        {
            _autoClose.Stop();
            if (!_answered)
            {
                _answered = true;
                ActionTaken?.Invoke(ReminderResponse.AutoDismiss);
            }
        };
    }

    private void Done_Click(object sender, RoutedEventArgs e) => Respond(ReminderResponse.Done);
    private void Snooze_Click(object sender, RoutedEventArgs e) => Respond(ReminderResponse.Snooze);

    private void Respond(ReminderResponse response)
    {
        if (_answered) return;
        _answered = true;
        _autoClose.Stop();
        ActionTaken?.Invoke(response);
        Close();
    }
}
