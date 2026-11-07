namespace Ergonomy;

public static class SideDockGeometry
{
    // Keep the activation band clear of title-bar close/maximize controls.
    public static double Offset(double workHeight,double scale) => 64*scale;
    public static bool IsHotspot(double x,double y,double right,double top,double height,double scale)
    {
        var bandTop=top+Offset(height,scale)+8*scale;
        return x>=right-4*scale&&x<right&&y>=bandTop&&y<=bandTop+60*scale;
    }
}
