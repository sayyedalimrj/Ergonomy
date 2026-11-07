namespace Ergonomy;
public enum ExerciseMotion { Blink,FarFocus,Sit,ChinGlide,Feet,Stand,Walk,Shrug,ShoulderRoll,NeckLeft,NeckRight,TiltLeft,TiltRight,UpperBack,Chest,WristLeft,WristRight,Hands,Ankles,Water }
public sealed record ExerciseStep(string Title,string Hint,int Seconds,ExerciseMotion Motion=ExerciseMotion.Sit,string Group="نشستن",string Source="");
public static class ExerciseCatalog
{
    private const string C="https://www.ccohs.ca/oshanswers/ergonomics/office/stretching.html";
    private const string N="https://www.mayoclinic.org/healthy-lifestyle/adult-health/multimedia/neck-stretches/vid-20084697";
    private const string H="https://www.newcastle-hospitals.nhs.uk/services/newcastle-occupational-health-service/information-for-staff/physiotherapy/self-help-leaflets/1-minute-body-check/";
    private const string W="https://www.osha.gov/etools/computer-workstations/checklists/evaluation";
    private const string E="https://www.aoa.org/healthy-eyes/eye-and-vision-conditions/computer-vision-syndrome";
    public static IReadOnlyList<ExerciseStep> All { get; } = new ExerciseStep[]{
        new("پلک‌زدن آرام","چشم‌ها را آرام باز و بسته کن؛ بدون فشار",5,ExerciseMotion.Blink,"چشم",E),
        new("نگاه به دوردست","۲۰ ثانیه به نقطه‌ای حدود ۶ متر دورتر نگاه کن",20,ExerciseMotion.FarFocus,"چشم",E),
        new("نشستن با تکیه‌گاه","کمر حمایت شود؛ شانه‌ها راحت باشند",8,ExerciseMotion.Sit,"نشستن",W),
        new("عقب‌بردن آرام چانه","بدون بالابردن چانه، سر را کمی عقب ببر",20,ExerciseMotion.ChinGlide,"گردن و شانه",C),
        new("حمایت از کف پا","کف پا را روی زمین یا زیرپایی قرار بده",8,ExerciseMotion.Feet,"نشستن",W),
        new("بلندشدن آرام","اگر تعادل داری، با آرامش از صندلی بلند شو",6,ExerciseMotion.Stand,"حرکت و پا",C),
        new("راه‌رفتن کوتاه","در مسیر امن اطراف خود چند قدم بردار",20,ExerciseMotion.Walk,"حرکت و پا",C),
        new("بالابردن و رهاکردن شانه‌ها","شانه‌ها را آرام بالا ببر، چند ثانیه نگه دار و رها کن",15,ExerciseMotion.Shrug,"گردن و شانه",C),
        new("چرخش آرام شانه‌ها","شانه‌ها را آهسته رو به عقب و سپس جلو بچرخان",15,ExerciseMotion.ShoulderRoll,"گردن و شانه",C),
        new("چرخش گردن به چپ","سر را آرام به چپ بچرخان؛ فقط تا محدودهٔ راحت",20,ExerciseMotion.NeckLeft,"گردن و شانه",N),
        new("چرخش گردن به راست","سر را آرام به راست بچرخان؛ شانه‌ها ثابت بمانند",20,ExerciseMotion.NeckRight,"گردن و شانه",N),
        new("کشش جانبی گردن به چپ","گوش چپ را به شانه نزدیک کن؛ شانه را بالا نبر",20,ExerciseMotion.TiltLeft,"گردن و شانه",N),
        new("کشش جانبی گردن به راست","گوش راست را به شانه نزدیک کن؛ بدون فشار دست",20,ExerciseMotion.TiltRight,"گردن و شانه",N),
        new("کشش ملایم بالای پشت","دست‌ها را جلو بیاور؛ پشت را آرام باز کن",10,ExerciseMotion.UpperBack,"بالاتنه",C),
        new("بازکردن سینه و آرنج‌ها","دست‌ها پشت سر؛ آرنج‌ها را در دامنهٔ راحت باز کن",10,ExerciseMotion.Chest,"بالاتنه",C),
        new("حرکت آرام مچ چپ","مچ دست چپ را آهسته بالا و پایین ببر",15,ExerciseMotion.WristLeft,"دست و مچ",C),
        new("حرکت آرام مچ راست","مچ دست راست را آهسته بالا و پایین ببر",15,ExerciseMotion.WristRight,"دست و مچ",C),
        new("باز و بسته‌کردن انگشتان","دست را آرام مشت کن و انگشتان را باز کن",10,ExerciseMotion.Hands,"دست و مچ",H),
        new("حرکت آرام مچ پا","روی صندلی ثابت، پنجه را بالا و پایین ببر؛ سپس پای دیگر",15,ExerciseMotion.Ankles,"حرکت و پا","https://www.nhs.uk/live-well/exercise/sitting-exercises/"),
        new("یادآوری نوشیدن آب","اگر نیاز داری، کمی آب بنوش",8,ExerciseMotion.Water,"آب","")
    };
    public static ExerciseStep Find(ExerciseMotion motion)=>All.First(x=>x.Motion==motion);
    public static IReadOnlyList<ExerciseStep> Routine(ReminderKind kind)=>kind switch {
        ReminderKind.Eye=>Pick(ExerciseMotion.Blink,ExerciseMotion.FarFocus),
        ReminderKind.Posture=>Pick(ExerciseMotion.Sit,ExerciseMotion.ChinGlide,ExerciseMotion.Feet),
        ReminderKind.Move=>Pick(ExerciseMotion.Stand,ExerciseMotion.Walk,ExerciseMotion.ShoulderRoll,ExerciseMotion.Hands),
        _=>Pick(ExerciseMotion.Water)
    };
    private static IReadOnlyList<ExerciseStep> Pick(params ExerciseMotion[] motions)=>motions.Select(Find).ToArray();
}
