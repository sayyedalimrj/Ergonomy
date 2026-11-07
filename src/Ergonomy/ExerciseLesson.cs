namespace Ergonomy;

public sealed record ExerciseStep(string Title, string Hint, int Seconds);

/// <summary>Explicit coaching sequence; elapsed time never marks a reminder Done.</summary>
public sealed class ExerciseLesson
{
    public ReminderKind Kind { get; }
    public IReadOnlyList<ExerciseStep> Steps { get; }
    public int Index { get; private set; }
    public double Elapsed { get; private set; }
    public bool Finished { get; private set; }
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
            ReminderKind.Posture => [new("شانه‌ها را رها کن", "راحت و بدون فشار",6),new("گردن در امتداد بدن", "چانه را کمی جمع کن؛ بدون درد",6),new("پاها را تکیه بده", "کف پا روی زمین یا زیرپایی",6)],
            ReminderKind.Move => [new("آرام از جا بلند شو", "اگر برایت راحت و ایمن است",6),new("چند قدم راه برو", "در فضای امن اطرافت",10),new("شانه‌ها را آرام حرکت بده", "در دامنهٔ راحت، بدون درد",8)],
            _ => [new("یک جرعه آب؟", "اگر نیاز داری، کمی بنوش",8)]
        };
    }
    public void Advance(double seconds)
    {
        if(!Running || Finished || !double.IsFinite(seconds) || seconds <= 0 || seconds > 3) return;
        Elapsed += seconds;
        while(Elapsed >= Current.Seconds)
        {
            Elapsed -= Current.Seconds;
            if(Index + 1 < Steps.Count) Index++;
            else { Elapsed=Current.Seconds;Finished=true;Running=false;break; }
        }
    }
}
