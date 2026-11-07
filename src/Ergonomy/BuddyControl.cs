using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

using Brush = System.Windows.Media.Brush;
using FontFamily = System.Windows.Media.FontFamily;
using Size = System.Windows.Size;
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
    private void DrawLesson(DrawingContext dc,ExerciseLesson lesson)
    {
        double scale=Math.Min(ActualWidth/276,ActualHeight/157);
        dc.PushTransform(new TranslateTransform((ActualWidth-276*scale)/2,0));dc.PushTransform(new ScaleTransform(scale,scale));
        var motion=lesson.Current.Motion;
        if(_lessonStep!=lesson.Index){_lessonStep=lesson.Index;_phase=0;}
        bool animated=!ReducedMotion&&SystemParameters.ClientAreaAnimation;
        double phase=animated?_phase:0;
        if(motion is ExerciseMotion.Blink or ExerciseMotion.FarFocus)DrawEyes(dc,motion,phase,animated);
        else if(motion==ExerciseMotion.ChinGlide)DrawChin(dc,phase,animated,lesson.Current.Seconds);
        else if(motion is ExerciseMotion.Stand or ExerciseMotion.Walk or ExerciseMotion.ShoulderRoll)DrawStanding(dc,motion,phase,animated,lesson.Current.Seconds);
        else DrawOfficeMotion(dc,motion,phase,animated,lesson.Current.Seconds);
        dc.Pop();dc.Pop();
    }
    private static void Line(DrawingContext dc,Brush color,double width,Point a,Point b)=>dc.DrawLine(new Pen(color,width){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},a,b);
    private void DrawEyes(DrawingContext dc,ExerciseMotion motion,double phase,bool animated)
    {
        var skin=Make("#F6D5BA");var ink=Make("#172940");var mint=Make("#83E8D0");var white=Make("#EDF7FF");
        dc.DrawEllipse(Make("#143844"),null,new Point(112,79),62,62);
        dc.DrawRoundedRectangle(mint,null,new Rect(67,125,90,27),18,18);
        dc.DrawEllipse(skin,null,new Point(112,72),43,48);dc.DrawRoundedRectangle(ink,null,new Rect(77,28,70,21),10,10);
        double cycle=phase%3.2;double eyelid=motion==ExerciseMotion.Blink&&animated&&cycle<.5?1-Math.Sin(cycle/.5*Math.PI):1;
        foreach(double x in new[]{95.0,129.0}){
            dc.DrawEllipse(white,null,new Point(x,69),9,Math.Max(.8,7*eyelid));
            dc.DrawEllipse(ink,null,new Point(x+(motion==ExerciseMotion.FarFocus?5:0),69),3.5,Math.Max(.6,4.5*eyelid));
        }
        Line(dc,ink,2,new Point(104,96),new Point(120,96));
        if(motion==ExerciseMotion.FarFocus){
            dc.DrawEllipse(null,new Pen(mint,2),new Point(230,60),17,17);dc.DrawEllipse(mint,null,new Point(230,60),4,4);
            dc.DrawLine(new Pen(Make("#769EAA"),1.5){DashStyle=DashStyles.Dot},new Point(158,69),new Point(209,60));Label(dc,"۶ متر",209,92,white,13);
        }
    }
    private void DrawChin(DrawingContext dc,double phase,bool animated,int seconds)
    {
        var skin=Make("#F6D5BA");var ink=Make("#172940");var purple=Make("#C5B2FF");var mint=Make("#83E8D0");
        double glide=animated?StretchEnvelope(phase,seconds):.75;
        dc.DrawEllipse(Make("#183849"),null,new Point(138,79),64,64);
        Line(dc,purple,32,new Point(137,97),new Point(137,135));
        Line(dc,skin,12,new Point(137,90),new Point(137,65));
        var head=new Point(144-10*glide,48);
        dc.DrawEllipse(skin,null,head,25,31);dc.DrawEllipse(ink,null,new Point(head.X-3,27),24,13);
        dc.DrawEllipse(skin,null,new Point(head.X+23,49),7,5);dc.DrawEllipse(ink,null,new Point(head.X+13,45),2.5,3);
        Line(dc,ink,1.5,new Point(head.X+12,65),new Point(head.X+22,65));
        dc.DrawLine(new Pen(mint,1){DashStyle=DashStyles.Dot},new Point(137,20),new Point(137,126));
        Arrow(dc,new Point(213,54),new Point(184,54),mint);Label(dc,"سر هم‌سطح",45,132,mint,11);
    }
    private void DrawStanding(DrawingContext dc,ExerciseMotion motion,double phase,bool animated,int seconds)
    {
        var blue=Make("#94C4FF");var skin=Make("#F6D5BA");var ink=Make("#263F56");var white=Make("#EDF7FF");var mint=Make("#83E8D0");
        bool rising=motion==ExerciseMotion.Stand,walking=motion==ExerciseMotion.Walk;
        double rise=rising?(animated?Math.Clamp(phase/seconds,0,1):.7):1;rise=rise*rise*(3-2*rise);
        var hip=new Point(129,106-20*rise);var shoulder=new Point(127,66-10*rise);
        var knee=new Point(158-24*rise,118);var ankle=new Point(159-24*rise,145);
        var otherKnee=new Point(knee.X-7,knee.Y-2);var otherAnkle=new Point(ankle.X-9,145);
        var hand=new Point(166-13*rise,98-6*rise);var otherHand=new Point(145,95-6*rise);
        if(rising){Line(dc,Make("#718A9B"),5,new Point(99,70),new Point(99,113));Line(dc,Make("#718A9B"),5,new Point(99,111),new Point(145,111));Line(dc,Make("#718A9B"),5,new Point(111,113),new Point(106,148));}
        if(walking){
            double stride=animated?Math.Sin(phase*3):.65;
            knee=new Point(hip.X+stride*16,117-Math.Max(0,stride)*8);ankle=new Point(hip.X+stride*29,145-Math.Max(0,stride)*5);
            otherKnee=new Point(hip.X-stride*16,117-Math.Max(0,-stride)*8);otherAnkle=new Point(hip.X-stride*29,145-Math.Max(0,-stride)*5);
            hand=new Point(shoulder.X-stride*26,96);otherHand=new Point(shoulder.X+stride*26,96);Arrow(dc,new Point(188,130),new Point(220,130),mint);
        }
        if(motion==ExerciseMotion.ShoulderRoll){
            double angle=animated?(phase<15?-phase:phase-15)*2*Math.PI/3:1;
            shoulder.X+=Math.Cos(angle)*3;shoulder.Y+=Math.Sin(angle)*4;
            hand=new Point(shoulder.X+24,shoulder.Y+33);otherHand=new Point(shoulder.X-15,shoulder.Y+32);
            var arc=new StreamGeometry();using(var g=arc.Open()){g.BeginFigure(new Point(194,63),false,false);g.ArcTo(new Point(176,46),new Size(16,16),0,true,SweepDirection.Clockwise,true,false);}arc.Freeze();dc.DrawGeometry(null,new Pen(mint,2),arc);
            Arrow(dc,new Point(176,46),new Point(184,44),mint);Label(dc,phase<15?"رو به عقب":"رو به جلو",186,91,mint,10);
        }
        Line(dc,Make("#678499"),1,new Point(68,151),new Point(213,151));
        Line(dc,ink,12,hip,otherKnee);Line(dc,ink,12,otherKnee,otherAnkle);Line(dc,white,5,otherAnkle,new Point(otherAnkle.X+12,otherAnkle.Y));
        Line(dc,ink,13,hip,knee);Line(dc,ink,13,knee,ankle);Line(dc,white,6,ankle,new Point(ankle.X+13,ankle.Y));
        Line(dc,blue,8,shoulder,otherHand);Line(dc,blue,27,shoulder,hip);
        var elbow=new Point((shoulder.X+hand.X)/2+4,(shoulder.Y+hand.Y)/2+5);Line(dc,blue,8,shoulder,elbow);Line(dc,blue,8,elbow,hand);dc.DrawEllipse(skin,null,hand,5,5);
        var head=new Point(shoulder.X,shoulder.Y-24);dc.DrawEllipse(skin,null,head,16,19);dc.DrawEllipse(ink,null,new Point(head.X+6,head.Y-2),2,2);dc.DrawEllipse(Make("#172940"),null,new Point(head.X,head.Y-12),13,7);
    }
    private void DrawOfficeMotion(DrawingContext dc,ExerciseMotion motion,double phase,bool animated,int seconds)
    {
        var skin=Make("#F6D5BA");var accent=Make("#BDA9FF");var mint=Make("#83E8D0");var blue=Make("#94C4FF");var ink=Make("#263F56");var white=Make("#F1F7FF");
        double q=animated?(1-Math.Cos(phase*2*Math.PI/3))/2:.65;
        bool held=motion is ExerciseMotion.NeckLeft or ExerciseMotion.NeckRight or ExerciseMotion.TiltLeft or ExerciseMotion.TiltRight or ExerciseMotion.ShoulderLeft or ExerciseMotion.ShoulderRight or ExerciseMotion.Chest or ExerciseMotion.UpperBack;
        if(held)q=animated?StretchEnvelope(phase,seconds):.75;
        if(motion is ExerciseMotion.WristLeft or ExerciseMotion.WristRight or ExerciseMotion.Hands){
            void Hand(double x,double y,double spread){
                dc.DrawRoundedRectangle(skin,null,new Rect(x-19,y,38,36),12,12);
                double[] lengths={22,29,27,20};for(int i=0;i<4;i++){
                    double px=x-13+i*9;var joint=new Point(px+(i-1.5)*spread*4,y-8-spread*lengths[i]*.4);var tip=new Point(px+(i-1.5)*spread*7,y-8-spread*lengths[i]);
                    Line(dc,skin,7,new Point(px,y+5),joint);Line(dc,skin,6,joint,tip);
                }Line(dc,skin,8,new Point(x-16,y+17),new Point(x-23-9*spread,y+6-8*spread));
            }
            if(motion==ExerciseMotion.Hands){
                dc.DrawRoundedRectangle(blue,null,new Rect(67,110,35,35),9,9);dc.DrawRoundedRectangle(accent,null,new Rect(172,110,35,35),9,9);Hand(85,79,q);Hand(190,79,q);
            }else{
                bool left=motion==ExerciseMotion.WristLeft;if(!left)dc.PushTransform(new ScaleTransform(-1,1,138,0));
                Line(dc,blue,27,new Point(55,106),new Point(136,106));dc.DrawEllipse(skin,null,new Point(148,105),12,12);dc.PushTransform(new RotateTransform(-15+30*q,148,105));Hand(150,68,.85);dc.Pop();Arrow(dc,new Point(208,64),new Point(208,101),mint);if(!left)dc.Pop();
                Label(dc,left?"دست چپ":"دست راست",213,132,mint,10);
            }return;
        }
        if(motion is ExerciseMotion.Ankles or ExerciseMotion.AnkleRight){
            // A whole seated figure keeps the raised leg connected to the hip, knee and ankle.
            Line(dc,Make("#718A9B"),5,new Point(95,58),new Point(95,113));Line(dc,Make("#718A9B"),5,new Point(95,111),new Point(143,111));Line(dc,Make("#718A9B"),5,new Point(104,113),new Point(98,149));
            Line(dc,ink,11,new Point(117,108),new Point(135,123));Line(dc,ink,11,new Point(135,123),new Point(135,146));Line(dc,white,5,new Point(135,146),new Point(147,146));
            Line(dc,blue,25,new Point(111,66),new Point(117,108));Line(dc,ink,13,new Point(117,108),new Point(157,111));Line(dc,ink,13,new Point(157,111),new Point(192,114));
            dc.PushTransform(new RotateTransform(-25+50*q,192,114));Line(dc,white,6,new Point(192,114),new Point(213,114));dc.Pop();
            var head=new Point(110,38);dc.DrawEllipse(skin,null,head,16,19);dc.DrawEllipse(Make("#172940"),null,new Point(110,26),14,7);dc.DrawEllipse(ink,null,new Point(117,37),2,2);
            Line(dc,blue,7,new Point(111,73),new Point(137,98));dc.DrawEllipse(skin,null,new Point(137,98),4,4);
            Arrow(dc,new Point(235,102),new Point(235,128),mint);Label(dc,motion==ExerciseMotion.Ankles?"پای چپ":"پای راست",222,27,mint,11);return;
        }
        if(motion==ExerciseMotion.UpperBack){
            // Side view: both hands reach forward, rather than crossing the forearms.
            double reach=25*q;Line(dc,ink,11,new Point(121,115),new Point(115,145));Line(dc,ink,11,new Point(136,115),new Point(143,145));
            Line(dc,accent,29,new Point(123,76),new Point(128,113));
            var head=new Point(126,44);dc.DrawEllipse(skin,null,head,18,22);dc.DrawEllipse(Make("#172940"),null,new Point(124,29),17,8);dc.DrawEllipse(ink,null,new Point(135,43),2,3);
            Line(dc,accent,8,new Point(125,79),new Point(153+reach*.5,88));Line(dc,accent,8,new Point(153+reach*.5,88),new Point(176+reach,88));
            Line(dc,accent,8,new Point(126,72),new Point(153+reach*.5,80));Line(dc,accent,8,new Point(153+reach*.5,80),new Point(176+reach,80));
            dc.DrawEllipse(skin,null,new Point(179+reach,84),8,8);Line(dc,skin,3,new Point(187+reach,77),new Point(187+reach,91));Arrow(dc,new Point(202,57),new Point(232,57),mint);return;
        }
        double shrug=0;if(motion==ExerciseMotion.Shrug){double t=phase%6;shrug=animated?(t<1?t:t<4?1:t<5?5-t:0):.7;}
        double shoulderY=76-7*shrug;
        dc.DrawEllipse(Make("#183849"),null,new Point(138,148),39,5);
        Line(dc,ink,11,new Point(126,116),new Point(122,144));Line(dc,ink,11,new Point(150,116),new Point(155,144));dc.DrawRoundedRectangle(accent,null,new Rect(111,shoulderY-9,54,50),17,17);
        var ls=new Point(112,shoulderY);var rs=new Point(164,shoulderY);var le=new Point(102,99);var re=new Point(174,99);var lh=new Point(106,121);var rh=new Point(170,121);
        if(motion==ExerciseMotion.Chest){le=new Point(100-12*q,82);re=new Point(176+12*q,82);lh=new Point(117,48);rh=new Point(159,48);}
        if(motion is ExerciseMotion.ShoulderLeft or ExerciseMotion.ShoulderRight){
            bool left=motion==ExerciseMotion.ShoulderLeft;
            if(!left)dc.PushTransform(new ScaleTransform(-1,1,138,0));
            // Left arm crosses the chest; the right hand supports below its elbow.
            le=new Point(126+18*q,86);lh=new Point(133+35*q,75);re=new Point(174,101);rh=new Point(le.X,le.Y+6);
            Line(dc,accent,8,ls,le);Line(dc,accent,8,le,lh);dc.DrawEllipse(skin,null,lh,5,5);
            Line(dc,accent,8,rs,re);Line(dc,accent,8,re,rh);dc.DrawEllipse(skin,null,rh,5,5);
            if(!left)dc.Pop();Label(dc,left?"بازوی چپ":"بازوی راست",215,129,mint,10);
        }else{
            Line(dc,accent,8,ls,le);Line(dc,accent,8,le,lh);Line(dc,accent,8,rs,re);Line(dc,accent,8,re,rh);dc.DrawEllipse(skin,null,lh,5,5);dc.DrawEllipse(skin,null,rh,5,5);
        }
        double direction=motion is ExerciseMotion.NeckLeft or ExerciseMotion.TiltLeft?-1:1;
        bool tilt=motion is ExerciseMotion.TiltLeft or ExerciseMotion.TiltRight;bool turn=motion is ExerciseMotion.NeckLeft or ExerciseMotion.NeckRight;
        if(tilt)dc.PushTransform(new RotateTransform(direction*12*q,138,65));if(turn){dc.PushTransform(new TranslateTransform(direction*3*q,0));dc.PushTransform(new ScaleTransform(1-.23*q,1,138,44));}
        dc.DrawEllipse(skin,null,new Point(138,44),18,22);dc.DrawEllipse(Make("#172940"),null,new Point(138,29),17,8);
        double gaze=turn?direction*7*q:0;foreach(double x in new[]{131.0,145.0}){dc.DrawEllipse(white,null,new Point(x+gaze,44),4,5);dc.DrawEllipse(ink,null,new Point(x+1+gaze,44),2,3);}Line(dc,ink,1.5,new Point(133+gaze,56),new Point(143+gaze,56));
        if(turn){dc.Pop();dc.Pop();}if(tilt)dc.Pop();
        if(turn||tilt)Arrow(dc,new Point(194,42),new Point(194+direction*23,42+(tilt?12:0)),mint);
        if(motion==ExerciseMotion.Shrug){Arrow(dc,new Point(90,86),new Point(90,65),mint);Arrow(dc,new Point(186,86),new Point(186,65),mint);}
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
