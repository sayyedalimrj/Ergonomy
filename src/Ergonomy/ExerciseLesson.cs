namespace Ergonomy;

public sealed record ExerciseStep(string Title, string Hint, int Seconds);

/// <summary>Explicit coaching sequence; elapsed time never marks a reminder Done.</summary>
public sealed class ExerciseLesson
{
    public ReminderKind Kind { get; }
    public IReadOnlyList<ExerciseStep> Steps { get; }
    public int Index { get; private set; }
    public double Elapsed { get; private set; }
    public bool StepComplete { get; private set; }
    public bool Finished => StepComplete && Index==Steps.Count-1;
    public bool Running { get; set; }
    public ExerciseStep Current => Steps[Index];
    public double Progress => Math.Clamp(Elapsed / Current.Seconds, 0, 1);
    public int Remaining => Math.Max(0, (int)Math.Ceiling(Current.Seconds - Elapsed));
    public ExerciseLesson(ReminderKind kind)
    {
        Kind = kind;
        Steps = kind switch
        {
            ReminderKind.Eye => [new("چند پلک آرام", "بدون فشار چشم‌ها را باز و بسته کن", 5),new("۲۰ ثانیه نگاه دور", "به نقطه‌ای حدود ۶ متر دورتر نگاه کن",20)],
            ReminderKind.Posture => [new("درست نشستی؟", "شانه‌ها رها؛ کمر و پاها حمایت شوند",8),new("گردنت راحت است؟", "سر در امتداد بدن؛ بدون فشار یا درد",8),new("پاها را تکیه بده", "کف پا روی زمین یا زیرپایی",6)],
            ReminderKind.Move => [new("آرام از جا بلند شو", "اگر برایت راحت و ایمن است",6),new("چند قدم راه برو", "در فضای امن اطرافت",10),new("شانه‌ها را آرام حرکت بده", "در دامنهٔ راحت، بدون درد",8)],
            _ => [new("آب خوردی؟", "اگر نیاز داری، کمی بنوش",8)]
        };
    }
    public void Advance(double seconds)
    {
        if(!Running || Finished || !double.IsFinite(seconds) || seconds <= 0 || seconds > 3) return;
        Elapsed += seconds;
        if(Elapsed >= Current.Seconds)
        {
            Elapsed=Current.Seconds;StepComplete=true;Running=false;
        }
    }
    public bool Next()
    {
        if(Index+1>=Steps.Count)return false;
        Select(Index+1);return true;
    }
    public bool Previous()
    {
        if(Index==0)return false;
        Select(Index-1);return true;
    }
    public void Restart() => Select(Index);
    private void Select(int index)
    {
        Index=index;Elapsed=0;StepComplete=false;Running=true;
    }
}
