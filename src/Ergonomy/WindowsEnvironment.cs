using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

using Point = System.Windows.Point;

namespace Ergonomy;

internal static class WindowsEnvironment
{
    [StructLayout(LayoutKind.Sequential)] private struct InputInfo { public uint Size, Time; }
    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public int Size; public Rect Monitor, Work; public uint Flags; }
    [DllImport("user32.dll")] private static extern bool GetLastInputInfo(ref InputInfo info);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern IntPtr GetShellWindow();
    [DllImport("user32.dll")] private static extern IntPtr GetDesktopWindow();
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);
    [DllImport("user32.dll")] private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);
    [DllImport("user32.dll")] private static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);
    [DllImport("shell32.dll")] private static extern int SHQueryUserNotificationState(out int state);
    [DllImport("user32.dll", EntryPoint="GetWindowLongW")] private static extern int GetWindowLong(IntPtr hwnd, int index);
    [DllImport("user32.dll", EntryPoint="SetWindowLongW")] private static extern int SetWindowLong(IntPtr hwnd, int index, int value);
    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X,Y; }
    [StructLayout(LayoutKind.Sequential)] private struct Margins { public int Left,Right,Top,Bottom; }
    [DllImport("user32.dll")] private static extern bool GetCursorPos(out NativePoint point);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(IntPtr hwnd);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr hwnd,IntPtr after,int x,int y,int width,int height,uint flags);
    [DllImport("dwmapi.dll")] private static extern int DwmSetWindowAttribute(IntPtr hwnd,int attribute,ref int value,int size);
    [DllImport("dwmapi.dll")] private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd,ref Margins margins);
    public static bool ApplyBackdrop(Window window)
    {
        var hwnd=new WindowInteropHelper(window).Handle;
        int dark=1,rounded=2,backdrop=3;
        DwmSetWindowAttribute(hwnd,20,ref dark,4);
        DwmSetWindowAttribute(hwnd,33,ref rounded,4);
        var margins=new Margins{Left=-1,Right=-1,Top=-1,Bottom=-1};
        DwmExtendFrameIntoClientArea(hwnd,ref margins);
        int result=DwmSetWindowAttribute(hwnd,38,ref backdrop,4);
        Diagnostics.Record("backdrop",result==0?"Native transient backdrop requested successfully":"Native backdrop unavailable; opaque contrast fallback");
        return result==0;
    }
    private static MonitorInfo Monitor(Window window)
    {
        var hwnd=new WindowInteropHelper(window).Handle;
        var m=new MonitorInfo{Size=Marshal.SizeOf<MonitorInfo>()};
        GetMonitorInfo(MonitorFromWindow(hwnd,2),ref m);return m;
    }
    public static (double Left,double Top,double Width,double AvailableHeight) CornerAnchor(Window window,double width)
    {
        var m=Monitor(window);var hwnd=new WindowInteropHelper(window).Handle;
        var dpi=Math.Max(96,GetDpiForWindow(hwnd))/96.0;
        var w=Math.Min(width,(m.Work.Right-m.Work.Left)/dpi-16);
        return (m.Work.Right/dpi-w-8,m.Work.Top/dpi+8,w,Math.Max(76,(m.Work.Bottom-m.Work.Top)/dpi-16));
    }
    public static void SnapCorner(Window window)
    {
        var hwnd=new WindowInteropHelper(window).Handle;var m=Monitor(window);
        var scale=Math.Max(96,GetDpiForWindow(hwnd))/96.0;
        SetWindowPos(hwnd,IntPtr.Zero,m.Work.Right-(int)Math.Round(window.Width*scale)-8,m.Work.Top+8,0,0,0x0015);
    }
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x,int y);
    public static void TestCornerPointer(Window window,Action test)
    {
        GetCursorPos(out var before);var m=Monitor(window);
        try { if(!SetCursorPos(m.Work.Right-8,m.Work.Top+1))throw new InvalidOperationException("Test desktop cursor unavailable.");test(); }
        finally { SetCursorPos(before.X,before.Y); }
    }
    public static bool PointerInCorner(Window window)
    {
        if(!GetCursorPos(out var p))return false;
        var m=Monitor(window);var scale=Math.Max(96,GetDpiForWindow(new WindowInteropHelper(window).Handle))/96.0;
        return p.X>=m.Work.Right-128*scale && p.X<m.Work.Right && p.Y>=m.Work.Top && p.Y<=m.Work.Top+4*scale;
    }
    public static bool PointerOver(Window window)
    {
        return GetCursorPos(out var p)&&GetWindowRect(new WindowInteropHelper(window).Handle,out var r)
            && p.X>=r.Left&&p.X<r.Right&&p.Y>=r.Top&&p.Y<r.Bottom;
    }
    public static double IdleSeconds()
    {
        var info = new InputInfo { Size = (uint)Marshal.SizeOf<InputInfo>() };
        return GetLastInputInfo(ref info) ? unchecked((uint)Environment.TickCount - info.Time) / 1000.0 : 180;
    }
    public static bool ShouldSuppress()
    {
        // Public shell notification policy: busy, presentation, fullscreen D3D, quiet time.
        if (SHQueryUserNotificationState(out var state) == 0 && state is 2 or 3 or 4 or 6 or 7) return true;
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero || hwnd == GetShellWindow() || hwnd == GetDesktopWindow()) return false;
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        return GetWindowRect(hwnd, out var r) && GetMonitorInfo(MonitorFromWindow(hwnd, 2), ref info)
            && r.Left <= info.Monitor.Left && r.Top <= info.Monitor.Top
            && r.Right >= info.Monitor.Right && r.Bottom >= info.Monitor.Bottom;
    }
    public static void SetNoActivate(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        SetWindowLong(hwnd, -20, GetWindowLong(hwnd, -20) | 0x08000000 | 0x00000080);
    }
    public static void SetInteractive(Window window, bool interactive)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        var style = GetWindowLong(hwnd, -20);
        SetWindowLong(hwnd, -20, interactive ? style & ~0x08000000 : style | 0x08000000);
        if (interactive) window.Activate();
    }
    public static void ClampToMonitor(Window window, bool center = false)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        var info = new MonitorInfo { Size = Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(hwnd, 2), ref info)) return;
        // Native coordinates are physical pixels. Convert with the window's current DPI.
        var source = PresentationSource.FromVisual(window);
        var matrix = source?.CompositionTarget?.TransformFromDevice ?? System.Windows.Media.Matrix.Identity;
        var a = matrix.Transform(new Point(info.Work.Left, info.Work.Top));
        var b = matrix.Transform(new Point(info.Work.Right, info.Work.Bottom));
        window.Width = Math.Min(766, b.X - a.X - 16);
        window.MaxHeight = Math.Max(92, b.Y - a.Y - 16);
        window.Left = center ? a.X + (b.X-a.X-window.Width)/2 : Math.Clamp(window.Left, a.X, Math.Max(a.X,b.X-window.Width));
        window.Top = center ? a.Y + 6 : Math.Clamp(window.Top, a.Y, Math.Max(a.Y,b.Y-window.Height));
    }
}
