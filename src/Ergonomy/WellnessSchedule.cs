namespace Ergonomy;

public enum ReminderKind { Eye, Posture, Move, Water }
public enum ReminderResponse { Done, Snooze, Dismissed }

/// <summary>Deterministic active-time scheduler; no platform APIs or wall-clock dependency.</summary>
public sealed class WellnessSchedule
{
    private readonly Dictionary<ReminderKind, double> _last = new();
    private readonly Dictionary<ReminderKind, double> _deferred = new();
    private double _active;
    private double _clock;
    private double _lastAlert = -90;
    private double _pauseUntil;
    public ReminderKind? Pending { get; private set; }
    public AppSettings Settings { get; set; }
    public bool IsPaused => _clock < _pauseUntil;
    public double ActiveSeconds => _active;
    public WellnessSchedule(AppSettings settings)
    {
        Settings = settings;
        foreach (var k in Enum.GetValues<ReminderKind>()) { _last[k] = 0; _deferred[k] = 0; }
    }
    private IEnumerable<(ReminderKind Kind, bool Enabled, int Minutes)> Items()
    {
        yield return (ReminderKind.Move, Settings.MoveEnabled, Settings.MoveMinutes);
        yield return (ReminderKind.Eye, Settings.EyeEnabled, Settings.EyeMinutes);
        yield return (ReminderKind.Posture, Settings.PostureEnabled, Settings.PostureMinutes);
        yield return (ReminderKind.Water, Settings.WaterEnabled, Settings.WaterMinutes);
    }
    public (ReminderKind Kind, TimeSpan Remaining)? Next => Items().Where(x => x.Enabled)
        .Select(x => ((ReminderKind Kind, TimeSpan Remaining)?)(x.Kind,
            TimeSpan.FromSeconds(Math.Max(Math.Max(0, Math.Clamp(x.Minutes, 1, 480) * 60 - (_active - _last[x.Kind])), _deferred[x.Kind] - _clock))))
        .OrderBy(x => x!.Value.Remaining).FirstOrDefault();
    public ReminderKind? Advance(double seconds, bool inactive, bool quiet)
    {
        if (!double.IsFinite(seconds) || seconds < 0) return null;
        _clock += seconds;
        // A long gap is suspend/resume, never charge it as work.
        if (inactive || IsPaused || seconds > 5) return null;
        _active += seconds;
        if (quiet || Pending != null || _active - _lastAlert < 90) return null;
        foreach (var x in Items())
            if (x.Enabled && _clock >= _deferred[x.Kind] && _active - _last[x.Kind] >= Math.Clamp(x.Minutes, 1, 480) * 60)
            { Pending = x.Kind; _lastAlert = _active; return x.Kind; }
        return null;
    }
    public void Respond(ReminderKind kind, ReminderResponse response, bool practice = false)
    {
        if (practice) return;
        if (Pending != kind) return;
        Pending = null;
        if (response == ReminderResponse.Snooze) _deferred[kind] = _clock + 600;
        else { _last[kind] = _active; _deferred[kind] = 0; }
    }
    public void PauseFor(TimeSpan duration) => _pauseUntil = _clock + Math.Max(0, duration.TotalSeconds);
    public void Resume() => _pauseUntil = 0;
}
