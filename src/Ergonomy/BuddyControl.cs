using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

using Point = System.Windows.Point;
using Color = System.Windows.Media.Color;
using Pen = System.Windows.Media.Pen;
using Brushes = System.Windows.Media.Brushes;

namespace Ergonomy;


/// <summary>Original retained vector character. 12 fps only while visible, active and motion allowed.</summary>
public sealed class BuddyControl : FrameworkElement
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(83) };
    private double _phase;
    private BuddyKind _kind;
    private BuddyState _state;
    public BuddyKind Kind
    {
        get => _kind;
        set { if (_kind != value) { _kind = value; InvalidateVisual(); } }
    }
    public BuddyState State
    {
        get => _state;
        set { if (_state != value) { _state = value; InvalidateVisual(); } }
    }
    public bool ReducedMotion { get; set; }
    public bool Animate { get; set; } = true;
    public BuddyControl()
    {
        _timer.Tick += (_, _) => { _phase += .083; InvalidateVisual(); };
        Loaded += (_, _) => Refresh();
        Unloaded += (_, _) => _timer.Stop();
        IsVisibleChanged += (_, _) => Refresh();
        IsHitTestVisible = false;
        System.Windows.Automation.AutomationProperties.SetName(this, "همراه سلامت؛ انیمیشن تزئینی");
    }
    public void Refresh()
    {
        if (IsVisible && IsLoaded && Animate && !ReducedMotion && SystemParameters.ClientAreaAnimation) _timer.Start();
        else _timer.Stop();
        InvalidateVisual();
    }
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);
        double size = Math.Min(ActualWidth / 120, ActualHeight / 120);
        if (size <= 0) return;
        dc.PushTransform(new ScaleTransform(size, size));
        var motion = Animate && !ReducedMotion && SystemParameters.ClientAreaAnimation;
        var wave = motion ? Math.Sin(_phase * (State == BuddyState.Reminder ? 5 : 2)) : 0;
        var color = Kind switch { BuddyKind.Posture => "#BCA7FF", BuddyKind.Eye => "#6FE5D0", BuddyKind.Move => "#82B7FF", _ => "#F5C58D" };
        var accent = (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;
        var ink = new SolidColorBrush(Color.FromRgb(19, 32, 53));
        var limb = new Pen(accent, 8) { StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(35, 0, 0, 0)), null, new Point(60,110),27,4);
        dc.PushTransform(new TranslateTransform(0, motion ? wave*2 : 0));
        var walking = Kind == BuddyKind.Move && State is BuddyState.Reminder or BuddyState.Attention;
        dc.DrawLine(limb,new Point(49,84),new Point(44+(walking?wave*8:0),105));
        dc.DrawLine(limb,new Point(70,84),new Point(76-(walking?wave*8:0),105));
        dc.DrawRoundedRectangle(accent,null,new Rect(34,51,52,41),17,17);
        var raised = State is BuddyState.Happy or BuddyState.Completed or BuddyState.Reminder;
        dc.DrawLine(limb,new Point(37,61),new Point(21,raised?36+wave*6:80));
        dc.DrawLine(limb,new Point(83,61),new Point(99,raised?36-wave*6:80));
        dc.DrawRoundedRectangle(accent,null,new Rect(28,13,64,50),22,22);
        dc.DrawRoundedRectangle(ink,null,new Rect(37,25,46,27),11,11);
        var closed = State is BuddyState.Resting or BuddyState.Snoozed || motion && _phase % 4.4 < .17;
        foreach (var x in new[]{50.0,70.0})
            dc.DrawEllipse(Brushes.White,null,new Point(x,36),3,closed?.8:4);
        dc.DrawEllipse(ink,null,new Point(60,72),8,8);
        var badge = new Pen(Brushes.White,2.4) {StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
        if(Kind==BuddyKind.Posture) dc.DrawLine(badge,new Point(60,66),new Point(60,78));
        else if(Kind==BuddyKind.Move) {dc.DrawLine(badge,new Point(56,76),new Point(64,68));dc.DrawLine(badge,new Point(59,68),new Point(64,68));}
        else if(Kind==BuddyKind.Eye) dc.DrawEllipse(null,badge,new Point(60,72),5,3);
        else {dc.DrawLine(badge,new Point(56,72),new Point(64,72));dc.DrawLine(badge,new Point(60,68),new Point(60,76));}
        if(State==BuddyState.Completed) {dc.DrawLine(badge,new Point(47,17),new Point(56,22));dc.DrawLine(badge,new Point(56,22),new Point(72,10));}
        dc.Pop(); dc.Pop();
    }
}
