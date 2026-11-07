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


/// <summary>Original vector character. 30 fps only while visible, active and motion allowed.</summary>
public sealed class BuddyControl : FrameworkElement
{
    private readonly DispatcherTimer _timer = new(DispatcherPriority.Background) { Interval = TimeSpan.FromMilliseconds(33) };
    private long _lastFrame;
    private static readonly Dictionary<string,SolidColorBrush> Palette=new();
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
    private ExerciseLesson? _lesson;
    private int _lessonStep=-1;
    public ExerciseLesson? Lesson
    {
        get=>_lesson;
        set { _lesson=value;_phase=0;_lessonStep=-1;InvalidateVisual(); }
    }
    public void RestartPose(){_phase=0;_lessonStep=-1;Refresh();}
    public bool ReducedMotion { get; set; }
    public bool Animate { get; set; } = true;
    public BuddyControl()
    {
        FlowDirection=System.Windows.FlowDirection.LeftToRight;
        _timer.Tick += (_, _) => {
            long now=System.Diagnostics.Stopwatch.GetTimestamp();
            _phase+=Math.Clamp((now-_lastFrame)/(double)System.Diagnostics.Stopwatch.Frequency,0,.1);_lastFrame=now;InvalidateVisual();
        };
        Loaded += (_, _) => Refresh();
        Unloaded += (_, _) => _timer.Stop();
        IsVisibleChanged += (_, _) => Refresh();
        IsHitTestVisible = false;
        System.Windows.Automation.AutomationProperties.SetName(this, "همراه سلامت؛ نمایش حرکت آموزشی");
    }
    public void Refresh()
    {
        if (IsVisible && IsLoaded && Animate && !ReducedMotion && SystemParameters.ClientAreaAnimation)
        {if(!_timer.IsEnabled){_lastFrame=System.Diagnostics.Stopwatch.GetTimestamp();_timer.Start();}}
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
        var moving=!ReducedMotion&&SystemParameters.ClientAreaAnimation;
        double phase=moving?_phase:0;
        var motion=lesson.Current.Motion;
        if(_lessonStep!=lesson.Index){_lessonStep=lesson.Index;_phase=0;phase=0;}
        if(DrawOfficeMotion(dc,motion,phase,moving,lesson.Current.Seconds)) {dc.Pop();dc.Pop();return;}
        int stage=motion switch {ExerciseMotion.FarFocus or ExerciseMotion.ChinGlide or ExerciseMotion.Walk=>1,ExerciseMotion.Feet or ExerciseMotion.ShoulderRoll=>2,_=>0};
        if(motion is ExerciseMotion.Blink or ExerciseMotion.FarFocus)
        {
            // Eye lesson: a slow blink, then a stable far target. No eye-rolling exercise.
            dc.DrawEllipse(Make("#143844"),null,new Point(112,79),62,62);
            dc.DrawRoundedRectangle(accent,null,new Rect(67,125,90,27),18,18);
            dc.DrawEllipse(skin,null,new Point(112,72),43,48);
            dc.DrawRoundedRectangle(ink,null,new Rect(77,28,70,21),10,10);
            double blinkCycle=phase%3.2;
            double eyelid=stage==0&&moving&&blinkCycle<.5?1-Math.Sin(blinkCycle/.5*Math.PI):1;
            double look=stage==1?5:0;
            foreach(var x in new[]{95.0,129.0})
            {
                dc.DrawEllipse(white,null,new Point(x,69),9,Math.Max(.8,7*eyelid));dc.DrawEllipse(ink,null,new Point(x+look,69),3.5,Math.Max(.6,4.5*eyelid));
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

        }
        else
        {
            bool posture=motion is ExerciseMotion.Sit or ExerciseMotion.ChinGlide or ExerciseMotion.Feet;
            bool standing=motion is ExerciseMotion.Stand or ExerciseMotion.Walk or ExerciseMotion.ShoulderRoll;
            // Side-view chair and articulated hip/knee/ankle geometry makes sitting/standing legible.
            var chair=new Pen(Make("#6A8297"),5){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round};
            if(posture||stage==0){dc.DrawLine(chair,new Point(99,69),new Point(99,114));dc.DrawLine(chair,new Point(99,111),new Point(144,111));dc.DrawLine(chair,new Point(113,112),new Point(106,147));}
            double t=lesson.StepComplete?1:moving?Math.Clamp(phase/lesson.Current.Seconds,0,1):Math.Max(.65,lesson.Progress);
            double stand=standing?(stage==0?t*t*(3-2*t):1):0;
            var hip=new Point(128,104-stand*18);
            double chin=motion==ExerciseMotion.ChinGlide?(moving?StretchEnvelope(phase,lesson.Current.Seconds):.7):0;
            var shoulder=new Point(126,65-stand*11);
            double shoulderDrop=posture&&stage==0?(moving?(Math.Sin(phase*1.1)+1)*2:3):0;
            shoulder.Y+=shoulderDrop;
            var head=new Point(shoulder.X-6*chin,shoulder.Y-24);
            var knee=new Point(156-stand*23,117);
            var ankle=new Point(157-stand*22,144);
            var farKnee=new Point(knee.X-9,knee.Y-2);var farAnkle=new Point(ankle.X-10,ankle.Y);
            var wrist=new Point(165,96-stand*10);var farWrist=new Point(145,95-stand*10);
            if(standing&&stage==1)
            {
                double stride=moving?Math.Sin(phase*3):.65;
                knee.X=hip.X+stride*16;knee.Y=116-Math.Max(0,stride)*9;ankle.X=hip.X+stride*29;ankle.Y=144-Math.Max(0,stride)*5;
                farKnee.X=hip.X-stride*16;farKnee.Y=116-Math.Max(0,-stride)*9;farAnkle.X=hip.X-stride*29;farAnkle.Y=144-Math.Max(0,-stride)*5;
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
            var farElbow=new Point(shoulder.X+(farWrist.X-shoulder.X)*.55-4,shoulder.Y+(farWrist.Y-shoulder.Y)*.6+5);
            dc.DrawLine(arm,shoulder,farElbow);dc.DrawLine(arm,farElbow,farWrist);
            dc.DrawLine(body,shoulder,hip);
            var elbow=new Point(shoulder.X+(wrist.X-shoulder.X)*.55+4,shoulder.Y+(wrist.Y-shoulder.Y)*.6+5);
            dc.DrawLine(arm,shoulder,elbow);dc.DrawLine(arm,elbow,wrist);dc.DrawEllipse(skin,null,wrist,5,5);
            dc.DrawEllipse(skin,null,head,16,19);
            dc.DrawEllipse(ink,null,new Point(head.X+6,head.Y-2),2,2);
            dc.DrawEllipse(ink,null,new Point(head.X+1,head.Y-12),13,7);
            if(posture)
            {
                if(stage==0)Arrow(dc,new Point(178,54),new Point(178,76),purple);
                if(stage==1){dc.DrawLine(new Pen(purple,1){DashStyle=DashStyles.Dot},new Point(shoulder.X-26,head.Y-20),new Point(shoulder.X-26,hip.Y));Arrow(dc,new Point(174,35),new Point(155,35),purple);}
                if(stage==2){dc.DrawRoundedRectangle(Make("#426B65"),null,new Rect(143,148,38,5),2,2);Arrow(dc,new Point(184,125),new Point(184,145),mint);}
            }
            if(motion==ExerciseMotion.Water)
            {
                dc.DrawRoundedRectangle(white,null,new Rect(165,74,17,23),3,3);
                dc.DrawRoundedRectangle(mint,null,new Rect(168,84,11,10),1,1);
            }
        }
        dc.Pop();dc.Pop();
    }
    private bool DrawOfficeMotion(DrawingContext dc,ExerciseMotion motion,double phase,bool animated,int seconds)
    {
        if(motion is ExerciseMotion.Blink or ExerciseMotion.FarFocus or ExerciseMotion.Sit or ExerciseMotion.ChinGlide or ExerciseMotion.Feet or ExerciseMotion.Stand or ExerciseMotion.Walk or ExerciseMotion.ShoulderRoll or ExerciseMotion.Water)return false;
        var skin=Make("#F6D5BA");var accent=Make("#BDA9FF");var mint=Make("#83E8D0");var blue=Make("#94C4FF");var ink=Make("#14263C");var white=Make("#F1F7FF");
        double q=animated?(1-Math.Cos(phase*1.05))/2:.55;
        if(motion is ExerciseMotion.NeckLeft or ExerciseMotion.NeckRight or ExerciseMotion.TiltLeft or ExerciseMotion.TiltRight)
            q=animated?StretchEnvelope(phase,seconds):.7;
        void Line(Brush brush,double width,Point a,Point b)=>dc.DrawLine(new Pen(brush,width){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},a,b);
        if(motion is ExerciseMotion.WristLeft or ExerciseMotion.WristRight or ExerciseMotion.Hands)
        {
            void Hand(double x,double y,double spread)
            {
                dc.DrawRoundedRectangle(skin,null,new Rect(x-19,y,38,36),12,12);
                double[] lengths={22,29,27,20};
                for(int i=0;i<4;i++)
                {
                    double px=x-13+i*9;
                    var basePoint=new Point(px,y+5);
                    var joint=new Point(px+(i-1.5)*spread*4,y-8-spread*lengths[i]*.4);
                    var tip=new Point(px+(i-1.5)*spread*7,y-8-spread*lengths[i]);
                    Line(skin,7,basePoint,joint);Line(skin,6,joint,tip);
                }
                Line(skin,8,new Point(x-16,y+17),new Point(x-23-9*spread,y+6-8*spread));
            }
            if(motion==ExerciseMotion.Hands)
            {
                dc.DrawRoundedRectangle(blue,null,new Rect(67,110,35,35),9,9);dc.DrawRoundedRectangle(accent,null,new Rect(172,110,35,35),9,9);
                Hand(85,79,q);Hand(190,79,q);Arrow(dc,new Point(126,67),new Point(149,67),mint);
            }
            else
            {
                bool left=motion==ExerciseMotion.WristLeft;
                if(!left)dc.PushTransform(new ScaleTransform(-1,1,138,0));
                Line(blue,27,new Point(55,106),new Point(136,106));
                dc.DrawEllipse(skin,null,new Point(148,105),12,12);
                dc.PushTransform(new RotateTransform(-12+24*q,148,105));
                Hand(150,68,.85);dc.Pop();
                Arrow(dc,new Point(208,64),new Point(208,101),mint);
                if(!left)dc.Pop();
            }
            return true;
        }
        if(motion==ExerciseMotion.Ankles)
        {
            Line(Make("#6C8296"),5,new Point(79,50),new Point(79,93));Line(Make("#6C8296"),5,new Point(79,91),new Point(131,91));Line(blue,17,new Point(97,87),new Point(145,95));
            Line(ink,15,new Point(145,95),new Point(187,111));Line(white,7,new Point(187,111),new Point(210,111-16*q));
            Line(Make("#6A8297"),1,new Point(72,143),new Point(203,143));Arrow(dc,new Point(232,94),new Point(232,117),mint);return true;
        }
        // Front-facing upper body makes neck direction and shoulder symmetry clear.
        double shoulderY=motion==ExerciseMotion.Shrug?76-7*q:76;
        dc.DrawEllipse(Make("#132B3C"),null,new Point(138,148),39,5);
        Line(ink,11,new Point(126,116),new Point(122,144));Line(ink,11,new Point(150,116),new Point(155,144));
        dc.DrawRoundedRectangle(accent,null,new Rect(111,shoulderY-9,54,50),17,17);
        var lShoulder=new Point(112,shoulderY);var rShoulder=new Point(164,shoulderY);
        var lElbow=new Point(102,99);var rElbow=new Point(174,99);var lHand=new Point(106,121);var rHand=new Point(170,121);
        if(motion==ExerciseMotion.UpperBack)
        {
            lElbow=new Point(111,98);rElbow=new Point(165,98);lHand=new Point(145-5*q,91);rHand=new Point(131+5*q,91);
            Arrow(dc,new Point(93,84),new Point(71,84),mint);Arrow(dc,new Point(183,84),new Point(205,84),mint);
        }
        if(motion==ExerciseMotion.Chest)
        {
            lElbow=new Point(90-7*q,82);rElbow=new Point(186+7*q,82);lHand=new Point(117,48);rHand=new Point(159,48);
            Arrow(dc,new Point(95,101),new Point(72,101),mint);Arrow(dc,new Point(181,101),new Point(204,101),mint);
        }
        Line(accent,8,lShoulder,lElbow);Line(accent,8,lElbow,lHand);Line(accent,8,rShoulder,rElbow);Line(accent,8,rElbow,rHand);
        dc.DrawEllipse(skin,null,lHand,5,5);dc.DrawEllipse(skin,null,rHand,5,5);
        double direction=motion is ExerciseMotion.NeckLeft or ExerciseMotion.TiltLeft?-1:1;
        bool tilt=motion is ExerciseMotion.TiltLeft or ExerciseMotion.TiltRight;
        bool turn=motion is ExerciseMotion.NeckLeft or ExerciseMotion.NeckRight;
        if(tilt)dc.PushTransform(new RotateTransform(direction*12*q,138,65));
        if(turn){dc.PushTransform(new TranslateTransform(direction*3*q,0));dc.PushTransform(new ScaleTransform(1-.23*q,1,138,44));}
        dc.DrawEllipse(skin,null,new Point(138,44),18,22);
        dc.DrawEllipse(ink,null,new Point(138,29),17,8);
        double gaze=turn?direction*7*q:0;
        dc.DrawEllipse(white,null,new Point(131+gaze,44),4,5);dc.DrawEllipse(white,null,new Point(145+gaze,44),4,5);
        dc.DrawEllipse(ink,null,new Point(132+gaze,44),2,3);dc.DrawEllipse(ink,null,new Point(146+gaze,44),2,3);
        Line(ink,1.5,new Point(133+gaze,56),new Point(143+gaze,56));
        if(turn){dc.Pop();dc.Pop();}
        if(tilt)dc.Pop();
        if(turn||tilt)Arrow(dc,new Point(194,42),new Point(194+direction*23,42+(tilt?12:0)),mint);
        if(motion==ExerciseMotion.Shrug){Arrow(dc,new Point(90,86),new Point(90,65),mint);Arrow(dc,new Point(186,86),new Point(186,65),mint);}
        return true;
    }
    private static double StretchEnvelope(double phase,int seconds)
    {
        double t=Math.Clamp(Math.Min(phase/2,(seconds-phase)/2),0,1);
        return t*t*(3-2*t);
    }
    private static SolidColorBrush Make(string hex)
    {
        if(Palette.TryGetValue(hex,out var cached))return cached;
        var brush=(SolidColorBrush)new BrushConverter().ConvertFromString(hex)!;brush.Freeze();Palette[hex]=brush;return brush;
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
