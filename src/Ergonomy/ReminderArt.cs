using System.Windows;
using System.Windows.Media;
using Brush=System.Windows.Media.Brush;
using Brushes=System.Windows.Media.Brushes;
using Color=System.Windows.Media.Color;
using Pen=System.Windows.Media.Pen;
using Point=System.Windows.Point;
namespace Ergonomy;
/// <summary>Informational illustrations, deliberately separate from timed exercise coaching.</summary>
public sealed class ReminderArt:FrameworkElement
{
    public ReminderKind Kind {get;set;}
    public ReminderArt(){FlowDirection=System.Windows.FlowDirection.LeftToRight;IsHitTestVisible=false;}
    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);double s=Math.Min(ActualWidth/240,ActualHeight/135);dc.PushTransform(new ScaleTransform(s,s));
        Brush mint=Brushes.MediumTurquoise,ink=new SolidColorBrush(Color.FromRgb(38,63,86)),blue=new SolidColorBrush(Color.FromRgb(148,196,255)),skin=new SolidColorBrush(Color.FromRgb(246,213,186));
        void Line(Brush b,double w,Point a,Point c)=>dc.DrawLine(new Pen(b,w){StartLineCap=PenLineCap.Round,EndLineCap=PenLineCap.Round},a,c);
        if(Kind==ReminderKind.Water){
            var cup=new StreamGeometry();using(var g=cup.Open()){g.BeginFigure(new Point(82,30),true,true);g.LineTo(new Point(158,30),true,false);g.LineTo(new Point(149,116),true,false);g.LineTo(new Point(91,116),true,false);}cup.Freeze();
            dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(35,131,232,208)),null,new Point(120,72),67,59);
            dc.DrawGeometry(new SolidColorBrush(Color.FromArgb(25,220,245,255)),new Pen(new SolidColorBrush(Color.FromRgb(195,232,245)),2),cup);
            dc.PushClip(cup);dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromArgb(190,84,199,228)),null,new Rect(75,65,90,60),8,8);dc.DrawEllipse(new SolidColorBrush(Color.FromRgb(131,232,208)),null,new Point(120,65),39,5);dc.Pop();
            Line(Brushes.White,3,new Point(92,43),new Point(96,64));dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(80,245,255,255)),null,new Point(131,87),3,3);
        }else{
            Line(ink,5,new Point(83,53),new Point(83,97));Line(ink,5,new Point(83,95),new Point(131,95));Line(ink,5,new Point(93,97),new Point(88,124));
            Line(blue,24,new Point(106,57),new Point(109,91));Line(ink,11,new Point(109,91),new Point(143,94));Line(ink,11,new Point(143,94),new Point(143,121));Line(mint,5,new Point(143,122),new Point(158,122));
            dc.DrawEllipse(skin,null,new Point(106,31),16,18);dc.DrawEllipse(ink,null,new Point(106,20),14,7);dc.DrawEllipse(ink,null,new Point(113,31),2,2);
            Line(blue,7,new Point(109,64),new Point(137,79));Line(mint,3,new Point(169,52),new Point(176,59));Line(mint,3,new Point(176,59),new Point(190,43));
        }
        dc.Pop();
    }
}
