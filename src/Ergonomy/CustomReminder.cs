namespace Ergonomy;
public sealed class CustomReminder
{
    public string Id { get; set; }=Guid.NewGuid().ToString("N");
    public string Title { get; set; }="";
    public string Message { get; set; }="";
    public int Minutes { get; set; }=60;
    [System.Text.Json.Serialization.JsonIgnore]
    public string IntervalText
    {
        get=>string.Concat(Minutes.ToString().Select(c=>(char)('۰'+c-'0')));
        set=>Minutes=SettingsValidation.Minutes(value,1440);
    }
    public bool Enabled { get; set; }=true;
}
public static class SettingsValidation
{
    public static int Minutes(string text,int maximum=480)
    {
        string normalized=string.Concat(text.Trim().Select(c=>char.GetNumericValue(c) is >=0 and <=9?((int)char.GetNumericValue(c)).ToString():c.ToString()));
        if(!int.TryParse(normalized,out var n)||n<1||n>maximum)throw new ArgumentException($"زمان باید بین ۱ و {string.Concat(maximum.ToString().Select(c=>(char)('۰'+c-'0')))} دقیقه باشد.");
        return n;
    }
    public static void Normalize(AppSettings s)
    {
        s.EyeMinutes=Math.Clamp(s.EyeMinutes,1,480);s.PostureMinutes=Math.Clamp(s.PostureMinutes,1,480);
        s.MoveMinutes=Math.Clamp(s.MoveMinutes,1,480);s.WaterMinutes=Math.Clamp(s.WaterMinutes,1,480);
        var ids=new HashSet<string>();
        s.CustomReminders=(s.CustomReminders??[]).Where(x=>x!=null&&!string.IsNullOrWhiteSpace(x.Title)).Take(20).ToList();
        foreach(var r in s.CustomReminders)
        {
            if(!Guid.TryParse(r.Id,out _)||!ids.Add(r.Id)){r.Id=Guid.NewGuid().ToString("N");ids.Add(r.Id);}
            r.Title=r.Title.Trim()[..Math.Min(r.Title.Trim().Length,60)];
            r.Message=(r.Message??"").Trim();if(r.Message.Length>160)r.Message=r.Message[..160];
            r.Minutes=Math.Clamp(r.Minutes,1,1440);
        }
    }
    public static void Validate(AppSettings s)
    {
        if(new[]{s.EyeMinutes,s.PostureMinutes,s.MoveMinutes,s.WaterMinutes}.Any(x=>x<1||x>480))throw new ArgumentException("زمان یادآورهای اصلی باید بین ۱ و ۴۸۰ دقیقه باشد.");
        if(s.CustomReminders.Count>20)throw new ArgumentException("حداکثر ۲۰ یادآور شخصی می‌توان افزود.");
        if(s.CustomReminders.Any(x=>string.IsNullOrWhiteSpace(x.Title)||x.Title.Length>60||x.Message.Length>160||x.Minutes<1||x.Minutes>1440))throw new ArgumentException("عنوان و زمان یادآور شخصی را بررسی کن.");
        if(s.CustomReminders.Select(x=>x.Id).Distinct().Count()!=s.CustomReminders.Count)throw new ArgumentException("شناسهٔ یادآور تکراری است.");
    }
}
