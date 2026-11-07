using System.Windows.Threading;
using Microsoft.Win32;

namespace Ergonomy;

public sealed class ReminderEngine : IDisposable
{
    private readonly DispatcherTimer _tick = new() { Interval = TimeSpan.FromSeconds(2) };
    private readonly WellnessSchedule _schedule;
    private long _lastTick = Environment.TickCount64;
    private bool _locked;
    private bool _disposed;
    public event Action<ReminderKind>? ReminderDue;
    public bool IsPaused => _schedule.IsPaused;
    public bool IsIdle => _locked || WindowsEnvironment.IdleSeconds() >= 180;
    public bool IsQuiet => WindowsEnvironment.ShouldSuppress();
    public (ReminderKind Kind, TimeSpan Remaining)? NextReminder => _schedule.Next;
    public ReminderEngine(AppSettings settings)
    {
        _schedule = new(settings);
        _tick.Tick += Tick;
        SystemEvents.SessionSwitch += SessionChanged;
        SystemEvents.PowerModeChanged += PowerChanged;
    }
    private void SessionChanged(object sender, SessionSwitchEventArgs e) =>
        _tick.Dispatcher.BeginInvoke((Action)(() => {
            if (e.Reason == SessionSwitchReason.SessionLock) _locked = true;
            if (e.Reason == SessionSwitchReason.SessionUnlock) _locked = false;
            _lastTick = Environment.TickCount64;
        }));
    private void PowerChanged(object sender, PowerModeChangedEventArgs e) =>
        _tick.Dispatcher.BeginInvoke((Action)(() => _lastTick = Environment.TickCount64));
    public void Start() { _lastTick = Environment.TickCount64; _tick.Start(); }
    public void Configure(AppSettings settings) => _schedule.Settings = settings;
    public void PauseFor(TimeSpan duration) => _schedule.PauseFor(duration);
    public void Resume() => _schedule.Resume();
    public void Respond(ReminderKind kind, ReminderResponse response, bool practice = false) => _schedule.Respond(kind, response, practice);
    private void Tick(object? sender, EventArgs e)
    {
        var now = Environment.TickCount64;
        var elapsed = (now - _lastTick) / 1000.0;
        _lastTick = now;
        var due = _schedule.Advance(elapsed, IsIdle, IsQuiet);
        if (due is ReminderKind kind) ReminderDue?.Invoke(kind);
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _tick.Stop(); _tick.Tick -= Tick;
        SystemEvents.SessionSwitch -= SessionChanged;
        SystemEvents.PowerModeChanged -= PowerChanged;
    }
}
