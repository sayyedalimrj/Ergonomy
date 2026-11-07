using System.Runtime.InteropServices;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Ergonomy;

public enum ReminderKind { Eye, Posture, Move, Water }
public enum ReminderResponse { Done, Snooze, AutoDismiss }

public sealed class ReminderEngine : IDisposable
{
    private readonly DispatcherTimer _timer;
    private readonly Dictionary<ReminderKind, double> _lastShown = new();
    private AppSettings _settings;
    private double _workSeconds;
    private double _lastAnyShown = -120;
    private long _lastTick;
    private DateTimeOffset _snoozeUntil = DateTimeOffset.MinValue;
    private bool _pending;
    private bool _sessionLocked;
    public event Action<ReminderKind>? ReminderDue;

    public ReminderEngine(AppSettings settings)
    {
        _settings = settings;
        foreach (ReminderKind kind in Enum.GetValues<ReminderKind>()) _lastShown[kind] = 0;
        _lastTick = Environment.TickCount64;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _timer.Tick += Tick;
        SystemEvents.SessionSwitch += SessionChanged;
    }

    private void SessionChanged(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionLock) _sessionLocked = true;
        if (e.Reason == SessionSwitchReason.SessionUnlock) _sessionLocked = false;
    }

    public void Start() => _timer.Start();
    public void Configure(AppSettings settings) => _settings = settings;

    private void Tick(object? sender, EventArgs e)
    {
        var now = Environment.TickCount64;
        double seconds = Math.Clamp((now - _lastTick) / 1000.0, 0, 5);
        _lastTick = now;
        if (_sessionLocked || IdleSeconds() >= 180) return;
        _workSeconds += seconds;
        if (_pending || DateTimeOffset.UtcNow < _snoozeUntil || _workSeconds - _lastAnyShown < 90) return;

        foreach (var (kind, enabled, minutes) in new[]
        {
            (ReminderKind.Move, _settings.MoveEnabled, _settings.MoveMinutes),
            (ReminderKind.Eye, _settings.EyeEnabled, _settings.EyeMinutes),
            (ReminderKind.Posture, _settings.PostureEnabled, _settings.PostureMinutes),
            (ReminderKind.Water, _settings.WaterEnabled, _settings.WaterMinutes)
        })
        {
            if (!enabled || _workSeconds - _lastShown[kind] < minutes * 60) continue;
            _pending = true;
            _lastAnyShown = _workSeconds;
            ReminderDue?.Invoke(kind);
            break;
        }
    }

    public void Respond(ReminderKind kind, ReminderResponse response)
    {
        _pending = false;
        if (response == ReminderResponse.Snooze)
            _snoozeUntil = DateTimeOffset.UtcNow.AddMinutes(10);
        else
            _lastShown[kind] = _workSeconds;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo
    {
        public uint cbSize;
        public uint dwTime;
    }
    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LastInputInfo info);

    private static double IdleSeconds()
    {
        var info = new LastInputInfo { cbSize = (uint)Marshal.SizeOf<LastInputInfo>() };
        if (!GetLastInputInfo(ref info)) return 180;
        return unchecked((uint)Environment.TickCount - info.dwTime) / 1000.0;
    }

    public void Dispose()
    {
        _timer.Stop();
        SystemEvents.SessionSwitch -= SessionChanged;
    }
}
