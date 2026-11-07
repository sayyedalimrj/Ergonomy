using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using Brushes=System.Windows.Media.Brushes;
using Color=System.Windows.Media.Color;
namespace Ergonomy;
public sealed class EdgeHandleWindow : Window
{
    public EdgeHandleWindow()
    {
        Width=16;Height=56;WindowStyle=WindowStyle.None;ResizeMode=ResizeMode.NoResize;
        AllowsTransparency=true;Background=Brushes.Transparent;Topmost=true;ShowInTaskbar=false;ShowActivated=false;
        Title="نشان همیار سلامت";
        var mark=new Border{Width=4,Height=36,CornerRadius=new CornerRadius(3),Background=new SolidColorBrush(Color.FromArgb(125,131,232,208)),HorizontalAlignment=System.Windows.HorizontalAlignment.Right,Margin=new Thickness(0,0,3,0),ToolTip="همیار سلامت؛ برای بازشدن مکث کن"};
        mark.Effect=new DropShadowEffect{Color=Color.FromRgb(9,18,33),BlurRadius=10,ShadowDepth=2,Opacity=.55};
        Content=mark;
        System.Windows.Automation.AutomationProperties.SetName(this,"نشان بازکردن همیار سلامت");
        SourceInitialized+=(_,_)=>WindowsEnvironment.SetNoActivate(this);
    }
}
