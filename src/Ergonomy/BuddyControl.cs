using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

using Brush = System.Windows.Media.Brush;
using FontFamily = System.Windows.Media.FontFamily;
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
    internal double AnimationClock => _phase;
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
    public ExerciseLesson? Lesson { get; set; }
    public bool ReducedMotion { get; set; }
    public bool Animate { get; set; } = true;
    public BuddyControl()
    {
        FlowDirection=System.Windows.FlowDirection.LeftToRight;
        _timer.Tick += (_, _) => { _phase += .083; InvalidateVisual(); };
        Loaded += (_, _) => Refresh();
        Unloaded += (_, _) => _timer.Stop();
        IsVisibleChanged += (_, _) => Refresh();
        IsHitTestVisible = false;
        System.Windows.Automation.AutomationProperties.SetName(this, "همراه سلامت؛ نمایش حرکت آموزشی");
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
        if(Lesson!=null){DrawLesson(dc,Lesson);return;}
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
    private void DrawLesson(DrawingContext dc, ExerciseLesson lesson)
    {
        double scale=Math.Min(ActualWidth/276,ActualHeight/157);
        dc.PushTransform(new TranslateTransform((ActualWidth-276*scale)/2,0));
        dc.PushTransform(new ScaleTransform(scale,scale));
        var mint=Make("#83E8D0");var purple=Make("#C5B2FF");var blue=Make("#94C4FF");
        var skin=Make("#F6D5BA");var ink=Make("#172940");var white=Make("#EDF7FF");
        var accent=lesson.Kind==ReminderKind.Eye?mint:lesson.Kind==ReminderKind.Posture?purple:blue;
        var moving=Animate&&!ReducedMotion&&SystemParameters.ClientAreaAnimation&&lesson.Running;
        double phase=moving?_phase:0;
        int stage=lesson.Index;
        if(lesson.Kind==ReminderKind.Eye)
        {
            // Eye lesson: a slow blink, then a stable far target. No eye-rolling exercise.
            dc.DrawEllipse(Make("#143844"),null,new Point(112,79),62,62);
            dc.DrawRoundedRectangle(accent,null,new Rect(67,125,90,27),18,18);
            dc.DrawEllipse(skin,null,new Point(112,72),43,48);
            dc.DrawRoundedRectangle(ink,null,new Rect(77,28,70,21),10,10);
            bool blink=stage==0&&(moving?phase%2.2<.35:false);
            double look=stage==1?5:0;
            foreach(var x in new[]{95.0,129.0})
            {
                if(blink)dc.DrawLine(new Pen(ink,3){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},new Point(x-7,69),new Point(x+7,69));
                else {dc.DrawEllipse(white,null,new Point(x,69),9,7);dc.DrawEllipse(ink,null,new Point(x+look,69),3.5,4.5);}
            }
            dc.DrawLine(new Pen(ink,2),new Point(104,96),new Point(120,96));
            if(stage==1)
            {
                dc.DrawEllipse(null,new Pen(mint,2),new Point(230,60),17,17);
                dc.DrawEllipse(mint,null,new Point(230,60),4,4);
                var dotted=new Pen(Make("#769EAA"),1.5){DashStyle=DashStyles.Dot};
                dc.DrawLine(dotted,new Point(158,69),new Point(209,60));
                Label(dc,"۶ متر",209,92,white,13);
            }
            else {Label(dc,"پلک آرام",191,70,mint,13);}
        }
        else
        {
            bool posture=lesson.Kind==ReminderKind.Posture;
            bool standing=lesson.Kind==ReminderKind.Move;
            // Side-view chair and articulated hip/knee/ankle geometry makes sitting/standing legible.
            var chair=new Pen(Make("#6A8297"),5){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
            if(posture||stage==0){dc.DrawLine(chair,new Point(99,69),new Point(99,114));dc.DrawLine(chair,new Point(99,111),new Point(144,111));dc.DrawLine(chair,new Point(113,112),new Point(106,147));}
            double stand=standing?(stage==0?(moving?(Math.Sin(phase*1.1-Math.PI/2)+1)/2:.65):1):0;
            var hip=new Point(128,104-stand*18);
            var shoulder=new Point(126+(posture&&stage==1?(moving?Math.Sin(phase*.9)*3:0):0),65-stand*11);
            double shoulderDrop=posture&&stage==0?(moving?(Math.Sin(phase*1.1)+1)*2:3):0;
            shoulder.Y+=shoulderDrop;
            var head=new Point(shoulder.X+(posture&&stage==1?(moving?(Math.Sin(phase*.9)+1)*3:0):0),shoulder.Y-24);
            var knee=new Point(156-stand*23,117);
            var ankle=new Point(157-stand*22,144);
            var farKnee=new Point(knee.X-9,knee.Y-2);var farAnkle=new Point(ankle.X-10,ankle.Y);
            var wrist=new Point(165,96-stand*10);var farWrist=new Point(145,95-stand*10);
            if(standing&&stage==1)
            {
                double stride=moving?Math.Sin(phase*3):.65;
                knee.X=hip.X+stride*16;knee.Y=114;ankle.X=hip.X+stride*29;
                farKnee.X=hip.X-stride*16;farKnee.Y=114;farAnkle.X=hip.X-stride*29;
                wrist=new Point(shoulder.X-stride*26,94);farWrist=new Point(shoulder.X+stride*26,94);
                dc.DrawLine(new Pen(Make("#769EAA"),1),new Point(67,150),new Point(212,150));
                Arrow(dc,new Point(182,130),new Point(220,130),blue);
            }
            if(standing&&stage==2)
            {
                double roll=moving?Math.Sin(phase*1.3):.4;
                shoulder.Y+=roll*3;
                wrist=new Point(shoulder.X+26,shoulder.Y+20+roll*10);
                farWrist=new Point(shoulder.X-26,shoulder.Y+20-roll*10);
                Arrow(dc,new Point(179,54),new Point(179,78),blue);
            }
            var leg=new Pen(ink,13){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
            var body=new Pen(accent,27){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
            var arm=new Pen(accent,8){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
            dc.DrawLine(leg,hip,farKnee);dc.DrawLine(leg,farKnee,farAnkle);
            dc.DrawLine(leg,hip,knee);dc.DrawLine(leg,knee,ankle);
            dc.DrawLine(new Pen(white,6){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},ankle,new Point(ankle.X+13,ankle.Y));
            dc.DrawLine(arm,shoulder,farWrist);
            dc.DrawLine(body,shoulder,hip);
            dc.DrawLine(arm,shoulder,wrist);dc.DrawEllipse(skin,null,wrist,5,5);
            dc.DrawEllipse(skin,null,head,16,19);
            dc.DrawEllipse(ink,null,new Point(head.X+6,head.Y-2),2,2);
            dc.DrawEllipse(ink,null,new Point(head.X+1,head.Y-12),13,7);
            if(posture)
            {
                if(stage==0)Arrow(dc,new Point(178,54),new Point(178,76),purple);
                if(stage==1){dc.DrawLine(new Pen(purple,1){DashStyle=DashStyles.Dot},new Point(shoulder.X-26,head.Y-20),new Point(shoulder.X-26,hip.Y));Arrow(dc,new Point(174,35),new Point(155,35),purple);}
                if(stage==2){dc.DrawRoundedRectangle(Make("#426B65"),null,new Rect(143,148,38,5),2,2);Arrow(dc,new Point(184,125),new Point(184,145),mint);}
            }
            if(lesson.Kind==ReminderKind.Water)
            {
                dc.DrawRoundedRectangle(white,null,new Rect(165,74,17,23),3,3);
                dc.DrawRoundedRectangle(mint,null,new Rect(168,84,11,10),1,1);
            }
        }
        dc.Pop();dc.Pop();
    }
    private static SolidColorBrush Make(string hex)
    {
        var brush=(SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;brush.Freeze();return brush;
    }
    private static void Arrow(DrawingContext dc,Point from,Point to,Brush color)
    {
        var pen=new Pen(color,2){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
        dc.DrawLine(pen,from,to);
        var v=from-to;v.Normalize();var normal=new Vector(-v.Y,v.X);
        dc.DrawLine(pen,to,to+v*6+normal*4);dc.DrawLine(pen,to,to+v*6-normal*4);
    }
    private void Label(DrawingContext dc,string text,double x,double y,Brush color,double size)
    {
        var font=new Typeface(new FontFamily(new Uri("pack://application:,,,/"),"./Assets/Fonts/#IRANYekanX"),FontStyles.Normal,FontWeights.Normal,FontStretches.Normal);
        dc.DrawText(new FormattedText(text,System.Globalization.CultureInfo.GetCultureInfo("fa-IR"),System.Windows.FlowDirection.RightToLeft,font,size,color,VisualTreeHelper.GetDpi(this).PixelsPerDip),new Point(x,y));
    }

}
