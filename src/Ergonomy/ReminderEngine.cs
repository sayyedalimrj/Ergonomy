using System.Runtime.InteropServices;
using System.Windows.Threading;
using Microsoft.Win32;

namespace Ergonomy;

public enum ReminderKind { Eye, Posture, Move, Water }
public enum ReminderResponse { Done, Snooze, Dismissed }

public sealed class ReminderEngine : IDisposable
{
    private readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly Dictionary<ReminderKind, double> _last = new();
    private AppSettings _settings;
    private double _activeSeconds;
    private double _lastAlertSeconds = -120;
    private long _lastTick = Environment.TickCount64;
    private DateTimeOffset _pausedUntil = DateTimeOffset.MinValue;
    private bool _sessionLocked;
    private bool _hasPendingAlert;
    private bool _subscribed;

    public event Action<ReminderKind>? ReminderDue;

    public ReminderEngine(AppSettings settings)
    {
        _settings = settings;
        foreach (ReminderKind kind in Enum.GetValues<ReminderKind>()) _last[kind] = 0;
        _tick.Tick += Tick;
        try { SystemEvents.SessionSwitch += SessionChanged; _subscribed = true; }
        catch (Exception ex) { Diagnostics.Record("session-hook", ex); }
    }

    public bool IsPaused => DateTimeOffset.UtcNow < _pausedUntil;
    public bool IsIdle => _sessionLocked || IdleSeconds() >= 180;

    public void Start() => _tick.Start();
    public void Configure(AppSettings settings) => _settings = settings;

    public (ReminderKind Kind, TimeSpan Remaining)? NextReminder
    {
        get
        {
            var choices = Schedule().Where(x => x.Enabled)
                .Select(x => (x.Kind, Remaining: TimeSpan.FromSeconds(
                    Math.Max(0, x.Minutes * 60 - (_activeSeconds - _last[x.Kind])))))
                .OrderBy(x => x.Remaining).ToList();
            return choices.Count == 0 ? null : choices[0];
        }
    }

    public void PauseFor(TimeSpan time)
    {
        _pausedUntil = DateTimeOffset.UtcNow.Add(time);
        _hasPendingAlert = false;
    }
    public void Resume() => _pausedUntil = DateTimeOffset.MinValue;

    private (ReminderKind Kind, bool Enabled, int Minutes)[] Schedule() =>
    [
        (ReminderKind.Move, _settings.MoveEnabled, _settings.MoveMinutes),
        (ReminderKind.Eye, _settings.EyeEnabled, _settings.EyeMinutes),
        (ReminderKind.Posture, _settings.PostureEnabled, _settings.PostureMinutes),
        (ReminderKind.Water, _settings.WaterEnabled, _settings.WaterMinutes)
    ];

    private void SessionChanged(object sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionLock) _sessionLocked = true;
        if (e.Reason == SessionSwitchReason.SessionUnlock) _sessionLocked = false;
    }

    private void Tick(object? sender, EventArgs e)
    {
        var now = Environment.TickCount64;
        var elapsed = Math.Clamp((now - _lastTick) / 1000.0, 0, 3);
        _lastTick = now;

        if (IsIdle || IsPaused || _hasPendingAlert) return;
        _activeSeconds += elapsed;
        if (_activeSeconds - _lastAlertSeconds < 90) return;

        foreach (var item in Schedule())
        {
            if (!item.Enabled || _activeSeconds - _last[item.Kind] < item.Minutes * 60) continue;
            _hasPendingAlert = true;
            _lastAlertSeconds = _activeSeconds;
            ReminderDue?.Invoke(item.Kind);
            break;
        }
    }

    public void Respond(ReminderKind kind, ReminderResponse response)
    {
        _hasPendingAlert = false;
        if (response == ReminderResponse.Snooze)
            PauseFor(TimeSpan.FromMinutes(10));
        else _last[kind] = _activeSeconds;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct LastInputInfo { public uint cbSize; public uint dwTime; }

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LastInputInfo info);

    private static double IdleSeconds()
    {
        try
        {
            var info = new LastInputInfo { cbSize = (uint)Marshal.SizeOf<LastInputInfo>() };
            if (!GetLastInputInfo(ref info)) return 0;
            return unchecked((uint)Environment.TickCount - info.dwTime) / 1000.0;
        }
        catch { return 0; }
    }

    public void Dispose()
    {
        _tick.Stop();
        if (_subscribed)
        {
            try { SystemEvents.SessionSwitch -= SessionChanged; } catch { }
        }
    }
}
