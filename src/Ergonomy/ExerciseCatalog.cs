namespace Ergonomy;
public enum ExerciseMotion { Blink,FarFocus,ChinGlide,Stand,Walk,Shrug,ShoulderRoll,NeckLeft,NeckRight,TiltLeft,TiltRight,UpperBack,Chest,WristLeft,WristRight,Hands,Ankles,AnkleRight,ShoulderLeft,ShoulderRight }
public sealed record ExerciseStep(string Title,string Hint,int Seconds,ExerciseMotion Motion,string Group,string Source);
public static class ExerciseCatalog
{
    private const string C="https://www.ccohs.ca/oshanswers/ergonomics/office/stretching.html";
    private const string N="https://www.mayoclinic.org/healthy-lifestyle/adult-health/multimedia/neck-stretches/vid-20084697";
    private const string S="https://www.mayoclinic.org/healthy-lifestyle/adult-health/multimedia/shoulder-stretches/vid-20084699";
    private const string E="https://www.aoa.org/healthy-eyes/eye-and-vision-conditions/computer-vision-syndrome";
    private const string A="https://www.nhs.uk/live-well/exercise/sitting-exercises/";
    public static IReadOnlyList<ExerciseStep> All { get; }=new ExerciseStep[]{
        new("پلک‌زدن کامل و آرام","چند بار پلک‌ها را کامل و آرام ببندید و باز کنید. چشم‌ها را فشار ندهید.",5,ExerciseMotion.Blink,"چشم",E),
        new("استراحت چشم با نگاه به دور","نگاهتان را از صفحه بردارید و ۲۰ ثانیه به نقطه‌ای در فاصلهٔ حدود ۶ متر نگاه کنید.",20,ExerciseMotion.FarFocus,"چشم",E),
        new("جمع‌کردن چانه","رو به جلو نگاه کنید و بدون پایین‌آوردن چانه، سر را کمی به عقب ببرید؛ سپس آرام رها کنید.",24,ExerciseMotion.ChinGlide,"گردن",C),
        new("بلندشدن از صندلی","صندلی را ثابت کنید. پاها را روی زمین بگذارید و با حفظ تعادل، آرام بلند شوید.",6,ExerciseMotion.Stand,"حرکت و پا",C),
        new("قدم‌زدن","از میز فاصله بگیرید و در مسیر بدون مانع چند قدم آرام راه بروید.",30,ExerciseMotion.Walk,"حرکت و پا",C),
        new("بالاانداختن و رهاکردن شانه‌ها","شانه‌ها را به سمت گوش‌ها بالا ببرید، ۳ ثانیه نگه دارید و رها کنید. ۳ بار تکرار کنید.",18,ExerciseMotion.Shrug,"شانه",C),
        new("چرخاندن شانه‌ها","شانه‌ها را ۵ بار آرام به عقب بچرخانید؛ سپس همین حرکت را رو به جلو انجام دهید.",30,ExerciseMotion.ShoulderRoll,"شانه",C),
        new("چرخش سر به چپ","سر را آهسته به چپ بچرخانید؛ شانه‌ها رو به جلو بمانند. در محدودهٔ راحت مکث کنید و برگردید.",20,ExerciseMotion.NeckLeft,"گردن",N),
        new("چرخش سر به راست","سر را آهسته به راست بچرخانید؛ شانه‌ها رو به جلو بمانند. در محدودهٔ راحت مکث کنید و برگردید.",20,ExerciseMotion.NeckRight,"گردن",N),
        new("کشش کنار گردن؛ چپ","گوش چپ را آرام به شانهٔ چپ نزدیک کنید؛ شانه را بالا نبرید و با دست فشار ندهید. مکث کنید و برگردید.",20,ExerciseMotion.TiltLeft,"گردن",N),
        new("کشش کنار گردن؛ راست","گوش راست را آرام به شانهٔ راست نزدیک کنید؛ شانه را بالا نبرید و با دست فشار ندهید. مکث کنید و برگردید.",20,ExerciseMotion.TiltRight,"گردن",N),
        new("کشش شانهٔ چپ","بازوی چپ را جلوی سینه به سمت راست ببرید. با دست راست زیر آرنج را حمایت کنید؛ تنه نچرخد.",20,ExerciseMotion.ShoulderLeft,"شانه",S),
        new("کشش شانهٔ راست","بازوی راست را جلوی سینه به سمت چپ ببرید. با دست چپ زیر آرنج را حمایت کنید؛ تنه نچرخد.",20,ExerciseMotion.ShoulderRight,"شانه",S),
        new("کشش دست‌ها به جلو","انگشتان را در هم قفل کنید و دست‌ها را رو به جلو ببرید؛ کف دست‌ها رو به بیرون و شانه‌ها پایین بمانند.",20,ExerciseMotion.UpperBack,"سینه و پشت",C),
        new("کشش عضلات سینه","دست‌ها را پشت سر بگذارید و آرنج‌ها را آرام باز کنید؛ کتف‌ها را به هم نزدیک کنید، بدون گودکردن کمر.",20,ExerciseMotion.Chest,"سینه و پشت",S),
        new("خم و راست‌کردن مچ چپ","ساعد چپ را جلو نگه دارید و مچ را آهسته بالا و پایین ببرید؛ انگشتان راحت باشند.",15,ExerciseMotion.WristLeft,"دست و مچ",C),
        new("خم و راست‌کردن مچ راست","ساعد راست را جلو نگه دارید و مچ را آهسته بالا و پایین ببرید؛ انگشتان راحت باشند.",15,ExerciseMotion.WristRight,"دست و مچ",C),
        new("باز و بسته‌کردن دست‌ها","دست‌ها را آرام مشت کنید؛ شست بیرون بماند. سپس انگشتان را باز کنید. چند بار تکرار کنید.",12,ExerciseMotion.Hands,"دست و مچ",C),
        new("بالا و پایین‌بردن پنجهٔ چپ","روی صندلی ثابت بنشینید. پای چپ را کمی از زمین بلند کنید و پنجه را آرام به طرف خود و سپس جلو ببرید.",15,ExerciseMotion.Ankles,"حرکت و پا",A),
        new("بالا و پایین‌بردن پنجهٔ راست","روی صندلی ثابت بنشینید. پای راست را کمی از زمین بلند کنید و پنجه را آرام به طرف خود و سپس جلو ببرید.",15,ExerciseMotion.AnkleRight,"حرکت و پا",A)
    };
    public static ExerciseStep Find(ExerciseMotion motion)=>All.First(x=>x.Motion==motion);
    public static IReadOnlyList<ExerciseStep> Routine(ReminderKind kind)=>kind switch {
        ReminderKind.Eye=>Pick(ExerciseMotion.Blink,ExerciseMotion.FarFocus),
        ReminderKind.Move=>Pick(ExerciseMotion.Stand,ExerciseMotion.Walk,ExerciseMotion.ShoulderRoll,ExerciseMotion.Hands),
        _=>throw new ArgumentException("این یادآور، برنامهٔ تمرین نیست.",nameof(kind))
    };
    private static IReadOnlyList<ExerciseStep> Pick(params ExerciseMotion[] motions)=>motions.Select(Find).ToArray();
}
