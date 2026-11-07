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
