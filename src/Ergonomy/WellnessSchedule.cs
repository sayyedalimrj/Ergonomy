namespace Ergonomy;
public enum ReminderKind { Eye, Posture, Move, Water, Custom }
public enum ReminderResponse { Done, Snooze, Dismissed, TimedOut }

/// <summary>One active-work scheduler for both built-in and user-created reminders.</summary>
public sealed class WellnessSchedule
{
    private readonly Dictionary<string,double> _last=new(),_deferred=new();
    private double _active,_clock,_lastAlert=-90,_pauseUntil;
    private string? _pendingKey;
    private AppSettings _settings;
    public ReminderKind? Pending { get; private set; }
    public CustomReminder? PendingCustom { get; private set; }
    public AppSettings Settings
    {
        get=>_settings;
        set { _settings=value;Synchronize(); }
    }
    public bool IsPaused=>_clock<_pauseUntil;
    public double ActiveSeconds=>_active;
    public WellnessSchedule(AppSettings settings){_settings=settings;Synchronize();}
    private IEnumerable<(string Key,ReminderKind Kind,bool Enabled,int Minutes,CustomReminder? Custom)> Items()
    {
        yield return ("Move",ReminderKind.Move,Settings.MoveEnabled,Settings.MoveMinutes,null);
        yield return ("Eye",ReminderKind.Eye,Settings.EyeEnabled,Settings.EyeMinutes,null);
        yield return ("Posture",ReminderKind.Posture,Settings.PostureEnabled,Settings.PostureMinutes,null);
        yield return ("Water",ReminderKind.Water,Settings.WaterEnabled,Settings.WaterMinutes,null);
        foreach(var r in Settings.CustomReminders??[])
            yield return ("user:"+r.Id,ReminderKind.Custom,r.Enabled,r.Minutes,r);
    }
    private void Synchronize()
    {
        var keys=Items().Select(x=>x.Key).ToHashSet();
        foreach(var key in keys)if(!_last.ContainsKey(key)){_last[key]=_active;_deferred[key]=0;}
        foreach(var key in _last.Keys.Where(x=>!keys.Contains(x)).ToList()){_last.Remove(key);_deferred.Remove(key);}
        if(_pendingKey!=null&&!Items().Any(x=>x.Key==_pendingKey&&x.Enabled)){Pending=null;PendingCustom=null;_pendingKey=null;}
    }
    private double Remaining(string key,int minutes)=>Math.Max(Math.Max(0,Math.Clamp(minutes,1,1440)*60-(_active-_last.GetValueOrDefault(key,_active))),_deferred.GetValueOrDefault(key)-_clock);
    public (ReminderKind Kind,TimeSpan Remaining)? Next=>Items().Where(x=>x.Enabled)
        .Select(x=>((ReminderKind Kind,TimeSpan Remaining)?)(x.Kind,TimeSpan.FromSeconds(Remaining(x.Key,x.Minutes))))
        .OrderBy(x=>x!.Value.Remaining).FirstOrDefault();
    public string NextTitle=>Items().Where(x=>x.Enabled).OrderBy(x=>Remaining(x.Key,x.Minutes))
        .Select(x=>x.Custom?.Title??(x.Kind switch{ReminderKind.Eye=>"چشم‌ها",ReminderKind.Posture=>"نشستن",ReminderKind.Move=>"حرکت",_=>"آب"})).FirstOrDefault()??"";
    public ReminderKind? Advance(double seconds,bool inactive,bool quiet)
    {
        if(!double.IsFinite(seconds)||seconds<0)return null;
        Synchronize();_clock+=seconds;
        if(inactive||IsPaused||seconds>5)return null;
        _active+=seconds;
        if(quiet||Pending!=null||_active-_lastAlert<90)return null;
        foreach(var x in Items().OrderBy(x=>_last[x.Key]+Math.Clamp(x.Minutes,1,1440)*60))
        {
            if(!x.Enabled||_clock<_deferred[x.Key]||_active-_last[x.Key]<Math.Clamp(x.Minutes,1,x.Kind==ReminderKind.Custom?1440:480)*60)continue;
            Pending=x.Kind;PendingCustom=x.Custom;_pendingKey=x.Key;_lastAlert=_active;return x.Kind;
        }
        return null;
    }
    public void Respond(ReminderKind kind,ReminderResponse response,bool practice=false)
    {
        if(practice||Pending!=kind||_pendingKey==null)return;
        if(response==ReminderResponse.Snooze)_deferred[_pendingKey]=_clock+600;
        else {_last[_pendingKey]=_active;_deferred[_pendingKey]=0;}
        _lastAlert=_active;
        Pending=null;PendingCustom=null;_pendingKey=null;
    }
    public void PauseFor(TimeSpan duration)=>_pauseUntil=_clock+Math.Max(0,duration.TotalSeconds);
    public void Resume()=>_pauseUntil=0;
}
