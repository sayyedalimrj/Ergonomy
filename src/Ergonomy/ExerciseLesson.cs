namespace Ergonomy;

/// <summary>Exercise duration, then 4 seconds to confirm/advance; final stage waits 5 seconds.
/// Automatic completion never asserts that the user performed the exercise.</summary>
public sealed class ExerciseLesson
{
    public ReminderKind Kind { get; }
    public IReadOnlyList<ExerciseStep> Steps { get; }
    public int Index { get; private set; }
    public double Elapsed { get; private set; }
    public double TransitionElapsed { get; private set; }
    public bool StepComplete { get; private set; }
    public bool IsLast=>Index==Steps.Count-1;
    public bool Finished=>StepComplete&&IsLast;
    public bool AutoFinished { get; private set; }
    public int TransitionSeconds=>IsLast?5:4;
    public int TransitionRemaining=>Math.Max(0,(int)Math.Ceiling(TransitionSeconds-TransitionElapsed));
    public bool Running { get; set; }
    public ExerciseStep Current=>Steps[Index];
    public double Progress=>Math.Clamp(Elapsed/Current.Seconds,0,1);
    public int Remaining=>Math.Max(0,(int)Math.Ceiling(Current.Seconds-Elapsed));
    public ExerciseLesson(ReminderKind kind):this(kind,ExerciseCatalog.Routine(kind),0) { }
    public ExerciseLesson(ReminderKind kind,IReadOnlyList<ExerciseStep> steps,int index=0)
    {
        if(steps.Count==0)throw new ArgumentException("فهرست تمرین خالی است.");
        Kind=kind;Steps=steps;Index=Math.Clamp(index,0,steps.Count-1);
    }
    public void Advance(double seconds)
    {
        if(!Running||AutoFinished||!double.IsFinite(seconds)||seconds<=0||seconds>3)return;
        if(!StepComplete)
        {
            double exerciseTime=Math.Min(seconds,Current.Seconds-Elapsed);
            Elapsed+=exerciseTime;seconds-=exerciseTime;
            if(Elapsed<Current.Seconds)return;
            StepComplete=true;
        }
        TransitionElapsed+=seconds;
        if(TransitionElapsed<TransitionSeconds)return;
        double carry=TransitionElapsed-TransitionSeconds;
        if(IsLast){AutoFinished=true;Running=false;return;}
        Select(Index+1);
        if(carry>0)Advance(carry);
    }
    public bool Next(){if(IsLast)return false;Select(Index+1);return true;}
    public bool Previous(){if(Index==0)return false;Select(Index-1);return true;}
    public void Restart()=>Select(Index);
    private void Select(int index)
    {
        Index=index;Elapsed=TransitionElapsed=0;StepComplete=AutoFinished=false;Running=true;
    }
}
