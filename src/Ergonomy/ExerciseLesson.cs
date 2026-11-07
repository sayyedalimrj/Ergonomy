namespace Ergonomy;


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
    public ExerciseLesson(ReminderKind kind) : this(kind,ExerciseCatalog.Routine(kind),0) { }
    public ExerciseLesson(ReminderKind kind,IReadOnlyList<ExerciseStep> steps,int index=0)
    {
        if(steps.Count==0)throw new ArgumentException("فهرست حرکت خالی است.");
        Kind=kind;Steps=steps;Index=Math.Clamp(index,0,steps.Count-1);
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
