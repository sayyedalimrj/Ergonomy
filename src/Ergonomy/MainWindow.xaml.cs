using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using Button=System.Windows.Controls.Button;
using TextBox=System.Windows.Controls.TextBox;
using ListBoxItem=System.Windows.Controls.ListBoxItem;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;
using Brush = System.Windows.Media.Brush;
using Size = System.Windows.Size;
using Application = System.Windows.Application;

namespace Ergonomy;

public partial class MainWindow : Window
{
    private AppSettings _settings;
    private List<CustomReminder> _draftCustom=[];
    private readonly ReminderEngine _engine;
    private readonly DispatcherTimer _tick = new() { Interval=TimeSpan.FromSeconds(1) };
    private readonly DispatcherTimer _pointer = new() { Interval=TimeSpan.FromMilliseconds(200) };
    private readonly CornerReveal _reveal = new();
    private readonly bool _diagnosticOnly;
    private readonly List<HistoryEntry> _history=HistoryStore.Load();
    private WinForms.NotifyIcon? _tray;
    private System.Drawing.Icon? _trayIcon;
    private EdgeHandleWindow? _edgeHint;
    private bool _library;
    private ReminderKind? _active;
    private ExerciseLesson? _lesson;
    private bool _practice, _closing, _preferences, _quiet, _shown, _initialHideCompleted, _quick, _about;
    private string _promptText="";
    private int _posturePrompt;
    private long _lastTick=Environment.TickCount64;
    private double _alertAge;
    private int _transition;
    private double _anchorTop, _anchorLeft;

    public MainWindow(bool diagnosticOnly=false)
    {
        _diagnosticOnly=diagnosticOnly;
        InitializeComponent();
        _settings=SettingsStore.Load();
        _engine=new ReminderEngine(_settings);
        _engine.ReminderDue+=kind=>ShowCoach(kind);
        _engine.CustomReminderDue+=custom=>ShowPersonal(custom);
        ExercisesList.ItemsSource=ExerciseCatalog.All;
        ExerciseFilter.SelectedIndex=0;
        SourceInitialized+=(_,_)=>{
            WindowsEnvironment.SetNoActivate(this);
            if(!WindowsEnvironment.ApplyBackdrop(this)) Glass.Background="#F218253A".ToBrush();
        };
        Loaded+=(_,_)=>{
            PositionCorner();
            if(!_diagnosticOnly&&!_initialHideCompleted) {
                _initialHideCompleted=true;
                Conceal(immediate:true);
                Diagnostics.Record("ui","Corner host initialized; hidden by default.");
            }
        };
        DpiChanged+=(_,_)=>Dispatcher.BeginInvoke((Action)PositionCorner);
        PreviewKeyDown+=(_,e)=>{if(e.Key==Key.Escape){Hide_Click(this,new RoutedEventArgs());e.Handled=true;}};
        PreviewMouseDown+=(_,_)=>WindowsEnvironment.SetInteractive(this,true);
        _tick.Tick+=Tick;
        _pointer.Tick+=PointerTick;
        SetMotion();
        if(!diagnosticOnly)
        {
            InitTray();
            SettingsStore.EnsureStartup(_settings.AutoStart);
            _quiet=_engine.IsQuiet;
            _edgeHint=new EdgeHandleWindow();
            _edgeHint.MouseLeftButtonUp+=(_,_)=>Reveal(false,true);
            UpdateEdgeHint();
            _engine.Start();_tick.Start();_pointer.Start();
            Microsoft.Win32.SystemEvents.DisplaySettingsChanged+=DisplayChanged;
        }
    }
    private void DisplayChanged(object? s,EventArgs e)=>Dispatcher.BeginInvoke((Action)PositionCorner);
    private void PositionCorner()
    {
        BeginAnimation(LeftProperty,null);
        var anchor=WindowsEnvironment.CornerAnchor(this,320);
        Width=anchor.Width;Left=anchor.Left;_anchorTop=anchor.Top;Top=_anchorTop;
        MaxHeight=anchor.AvailableHeight;
        WindowsEnvironment.SnapCorner(this);_anchorTop=Top;_anchorLeft=Left;
        if(_edgeHint?.IsVisible==true)WindowsEnvironment.PositionHandle(this,_edgeHint);
        BodyScroll.MaxHeight=Math.Max(100,MaxHeight-78);
    }
    private void InitTray()
    {
        try
        {
            using(var iconStream=Application.GetResourceStream(new Uri("pack://application:,,,/Assets/AppIcon.ico"))!.Stream)
            using(var sourceIcon=new System.Drawing.Icon(iconStream)) _trayIcon=(System.Drawing.Icon)sourceIcon.Clone();
            _tray=new WinForms.NotifyIcon{Icon=_trayIcon,Text="همیار سلامت",Visible=true};
            var menu=new WinForms.ContextMenuStrip();
            menu.Items.Add("نمایش",null,(_,_)=>Dispatcher.BeginInvoke((Action)(()=>Reveal(_active!=null,true))));
            menu.Items.Add("حرکت‌ها",null,(_,_)=>Dispatcher.BeginInvoke((Action)OpenLibrary));
            menu.Items.Add("توقف / ادامه",null,(_,_)=>Dispatcher.BeginInvoke((Action)(()=>{
                if(_engine.IsPaused)_engine.Resume();else{DismissActive();_engine.PauseFor(TimeSpan.FromHours(1));}
                UpdateNext();Conceal();
            })));
            menu.Items.Add("زمان‌بندی",null,(_,_)=>Dispatcher.BeginInvoke((Action)OpenPreferences));
            menu.Items.Add("درباره",null,(_,_)=>Dispatcher.BeginInvoke((Action)(()=>About_Click(this,new RoutedEventArgs()))));
            menu.Items.Add("خروج",null,(_,_)=>Dispatcher.BeginInvoke((Action)ExitApp));
            _tray.ContextMenuStrip=menu;
            _tray.DoubleClick+=(_,_)=>Dispatcher.BeginInvoke((Action)(()=>Reveal(_active!=null,true)));
        }
        catch(Exception ex){Diagnostics.Record("tray",ex);}
    }
    private void PointerTick(object? sender,EventArgs e)
    {
        var quiet=!_diagnosticOnly&&_engine.IsQuiet;
        _quiet=quiet;
        var hotspot=WindowsEnvironment.PointerInCorner(this)||(_edgeHint?.IsMouseOver??false);
        var over=IsVisible && WindowsEnvironment.PointerOver(this);
        bool wanted=_reveal.Update(.2,hotspot,over,_active!=null||_preferences||_about||_library,quiet);
        if(wanted && !_shown)Reveal(_active!=null);
        else if(!wanted && _shown)Conceal();
        UpdateEdgeHint();
    }
    private void UpdateEdgeHint()
    {
        if(_edgeHint==null)return;
        if(_quiet||_shown||_closing){_edgeHint.Hide();return;}
        if(!_edgeHint.IsVisible)_edgeHint.Show();
        WindowsEnvironment.PositionHandle(this,_edgeHint);
    }
    private void Reveal(bool lesson,bool interactive=false,bool animate=true)
    {
        if(_quiet && !interactive)return;
        bool entering=!_shown||!IsVisible;
        _transition++;
        _shown=true;_reveal.Reveal();UpdateEdgeHint();BodyScroll.ScrollToTop();
        LessonPanel.Visibility=lesson&&_lesson!=null&&!_quick&&!_about&&!_preferences&&!_library?Visibility.Visible:Visibility.Collapsed;
        PersonalPanel.Visibility=(_active is ReminderKind.Custom or ReminderKind.Posture or ReminderKind.Water)&&!_quick&&!_about&&!_preferences&&!_library?Visibility.Visible:Visibility.Collapsed;
        LibraryPanel.Visibility=_library?Visibility.Visible:Visibility.Collapsed;
        QuickActions.Visibility=_quick?Visibility.Visible:Visibility.Collapsed;
        AboutPanel.Visibility=_about?Visibility.Visible:Visibility.Collapsed;
        PreferencesPanel.Visibility=_preferences?Visibility.Visible:Visibility.Collapsed;
        PlusButton.Content=_quick?"×":"+";
        FitHeight();PositionCorner();
        WindowsEnvironment.SetInteractive(this,interactive);
        bool soften=animate&&entering&&MotionAllowed;
        BeginAnimation(LeftProperty,null);BeginAnimation(OpacityProperty,null);
        Opacity=soften?0:1;
        if(!IsVisible)Show();
        SetMotion();UpdateNext();
        if(soften)
        {
            var duration=TimeSpan.FromMilliseconds(interactive?550:750);
            BeginAnimation(LeftProperty,new DoubleAnimation(_anchorLeft+28,_anchorLeft,duration){EasingFunction=new SineEase{EasingMode=EasingMode.EaseInOut},FillBehavior=FillBehavior.Stop});
            int enteringTransition=_transition;
            var fade=new DoubleAnimation(0,1,duration){EasingFunction=new SineEase{EasingMode=EasingMode.EaseInOut},FillBehavior=FillBehavior.HoldEnd};
            fade.Completed+=(_,_)=>{if(enteringTransition==_transition&&_shown){Opacity=1;BeginAnimation(OpacityProperty,null);}};
            BeginAnimation(OpacityProperty,fade);
        }
    }
    private void FitHeight()
    {
        BodyScroll.InvalidateMeasure();LibraryPanel.InvalidateMeasure();PersonalPanel.InvalidateMeasure();
        LessonPanel.InvalidateMeasure();PreferencesPanel.InvalidateMeasure();
        QuickActions.InvalidateMeasure();AboutPanel.InvalidateMeasure();Glass.InvalidateMeasure();
        UpdateLayout();
        Glass.Measure(new Size(Width,double.PositiveInfinity));
        Height=Math.Min(MaxHeight>0 && double.IsFinite(MaxHeight)?MaxHeight:600,Math.Max(76,Glass.DesiredSize.Height));
    }
    private void Conceal(bool immediate=false)
    {
        double visibleOpacity=Opacity,visibleLeft=Left;
        _transition++;int transition=_transition;
        _shown=false;_reveal.Hide();_quick=false;_about=false;_library=false;
        QuickActions.Visibility=Visibility.Collapsed;AboutPanel.Visibility=Visibility.Collapsed;PlusButton.Content="+";
        CoachBuddy.Animate=DockBuddy.Animate=false;CoachBuddy.Refresh();DockBuddy.Refresh();
        BeginAnimation(LeftProperty,null);BeginAnimation(OpacityProperty,null);
        Left=visibleLeft;Opacity=visibleOpacity;
        if(immediate||!MotionAllowed||!IsVisible){Hide();Left=_anchorLeft;UpdateEdgeHint();return;}
        var slide=new DoubleAnimation(Left,_anchorLeft+40,TimeSpan.FromMilliseconds(400)){EasingFunction=new SineEase{EasingMode=EasingMode.EaseInOut},FillBehavior=FillBehavior.Stop};
        slide.Completed+=(_,_)=>{if(transition==_transition){Hide();Left=_anchorLeft;UpdateEdgeHint();}};
        BeginAnimation(LeftProperty,slide);
        BeginAnimation(OpacityProperty,new DoubleAnimation(visibleOpacity,0,TimeSpan.FromMilliseconds(400)){FillBehavior=FillBehavior.HoldEnd});
    }
    private bool MotionAllowed=>_settings.AnimationsEnabled&&!_settings.ReducedMotion&&SystemParameters.ClientAreaAnimation;
    private void SetMotion()
    {
        DockBuddy.Kind=_settings.Buddy;
        DockBuddy.Animate=false; // no decorative motion while waiting
        CoachBuddy.ReducedMotion=_settings.ReducedMotion;
        CoachBuddy.Animate=_shown&&!_quick&&!_about&&!_preferences&&!_library&&MotionAllowed&&!_quiet&&(_diagnosticOnly||!_engine.IsIdle)&&(_lesson is {Running:true,StepComplete:false});
        DockBuddy.Refresh();CoachBuddy.Refresh();
    }
    private void Tick(object? sender,EventArgs e)
    {
        long now=Environment.TickCount64;
        double elapsed=(now-_lastTick)/1000.0;_lastTick=now;
        if(elapsed>3)elapsed=0;
        // Diagnostic windows use a controlled quiet policy, like PointerTick; the
        // disposable runner's shell notification state must not pause timer tests.
        _quiet=!_diagnosticOnly&&_engine.IsQuiet;
        AdvanceVisibleSession(elapsed);
        if(_diagnosticOnly)Diagnostics.Record("dispatcher-test",$"Elapsed={elapsed:F3};Quiet={_quiet};Shown={_shown};Visible={IsVisible};Index={_lesson?.Index};StepComplete={_lesson?.StepComplete};Grace={_lesson?.TransitionElapsed:F3}");
        SetMotion();UpdateNext();
    }
    private void AdvanceVisibleSession(double elapsed)
    {
        if(!double.IsFinite(elapsed)||elapsed<=0||elapsed>3)return;
        if(_active!=null&&_shown&&IsVisible&&!_quiet&&(_diagnosticOnly||!_engine.IsIdle)&&!_quick&&!_about&&!_preferences&&!_library)
        {
            _alertAge+=elapsed;
            int previous=_lesson?.Index??-1;
            bool wasComplete=_lesson?.StepComplete??false;
            _lesson?.Advance(elapsed);
            if(_lesson is {AutoFinished:true}){Finish(ReminderResponse.TimedOut);return;}
            bool changed=_lesson!=null&&_lesson.Index!=previous;
            if(changed){_alertAge=0;CoachBuddy.RestartPose();BodyScroll.ScrollToTop();}
            if(_lesson==null){
                PromptTimeout.Text=$"جمع‌شدن خودکار تا {FaNumber(Math.Max(0,20-(int)_alertAge))} ثانیه";
                if(_alertAge>=20){Finish(ReminderResponse.TimedOut);return;}
            }
            UpdateLesson();
            if(changed||(_lesson!=null&&_lesson.StepComplete!=wasComplete))FitHeight();
        }
    }
    private void UpdateNext()
    {
        if(_active!=null){TitleText.Text=_promptText;NextText.Text=_lesson==null?"یک یادآوری کوتاه":"آرام و بدون عجله";return;}
        TitleText.Text="همیار سلامت";
        if(_engine.IsPaused){NextText.Text="فعلاً استراحت";return;}
        var next=_engine.NextReminder;
        NextText.Text=next is null?"همراه سلامت شما":$"{_engine.NextTitle} · {FaNumber(Math.Max(1,(int)Math.Ceiling(next.Value.Remaining.TotalMinutes)))} دقیقهٔ دیگر";
    }
    private void ShowCoach(ReminderKind kind,bool practice=false)
    {
        if(!practice&&(_preferences||_library||_about)){_engine.Respond(kind,ReminderResponse.Snooze);return;}
        if(_active!=null) {
            if(!practice){_engine.Respond(kind,ReminderResponse.Snooze);return;}
            Finish(ReminderResponse.Dismissed,hide:false);
        }
        _preferences=false;_quick=false;_about=false;_library=false;_active=kind;_practice=practice;_alertAge=0;_lastTick=Environment.TickCount64;
        _promptText=kind switch {
            ReminderKind.Eye=>"زمان استراحت چشم",
            ReminderKind.Posture=>"وضعیت نشستن",
            ReminderKind.Move=>"چند دقیقه تغییر فعالیت",_=>"وقت نوشیدن آب"
        };
        if(kind is ReminderKind.Posture or ReminderKind.Water)
        {
            _lesson=null;CoachBuddy.Lesson=null;
            if(kind==ReminderKind.Posture){
                var tips=new[]{("صاف و راحت بنشینید","کمرتان به پشتی صندلی تکیه داشته باشد و شانه‌ها را رها کنید؛ در یک حالت ثابت نمانید."),("گردن در راستای تنه","اگر به صفحه نزدیک شده‌اید، موقعیت مانیتور را تنظیم کنید تا سر رو به جلو خم نماند."),("شانه‌ها را رها کنید","آرنج‌ها را نزدیک بدن و موس را در دسترس قرار دهید؛ مچ‌ها در امتداد ساعد باشند."),("وضعیت پاها را بررسی کنید","کف پاها روی زمین یا زیرپایی قرار بگیرد؛ فضای زیر میز را برای حرکت پاها باز نگه دارید.")};
                var tip=tips[_posturePrompt++%tips.Length];_promptText=tip.Item1;PersonalMessageText.Text=tip.Item2;
            }else PersonalMessageText.Text="اگر مایلید، چند جرعه آب بنوشید.";
            PromptArt.Kind=kind;PromptArt.InvalidateVisual();PromptArt.Visibility=Visibility.Visible;
            PromptDoneButton.Content=kind==ReminderKind.Water?"نوشیدم ✓":"بررسی کردم ✓";
            PromptTimeout.Text="جمع‌شدن خودکار تا ۲۰ ثانیه";Reveal(false,practice);return;
        }
        _lesson=new ExerciseLesson(kind){Running=true};
        CoachBuddy.Lesson=_lesson;
        CoachBuddy.State=BuddyState.Reminder;
        CoachBuddy.Kind=kind switch{ReminderKind.Eye=>BuddyKind.Eye,ReminderKind.Posture=>BuddyKind.Posture,ReminderKind.Move=>BuddyKind.Move,_=>BuddyKind.Wellness};
        UpdateLesson();Reveal(true,practice);
    }
    private void UpdateLesson()
    {
        if(_lesson==null)return;
        StepTitle.Text=_lesson.Current.Title;
        StepHint.Text=_lesson.StepComplete?(_lesson.IsLast?(_lesson.Kind==ReminderKind.Move?"در صورت امکان، ۵ تا ۱۰ دقیقه فعالیتی دور از نمایشگر انجام دهید.":"تمرین به پایان رسید؛ اگر انجام دادید، تأیید کنید."):"اگر این حرکت را انجام دادید، تأیید کنید؛ حرکت بعدی خودکار نمایش داده می‌شود."):_lesson.Current.Hint;
        StepNumber.Text=$"{_lesson.Current.Group} · {FaNumber(_lesson.Index+1)} از {FaNumber(_lesson.Steps.Count)}";
        StepTitle.ToolTip="راهنمای حرکت: "+_lesson.Current.Source;
        Countdown.Text=_lesson.StepComplete?$"{(_lesson.IsLast?"پایان":"بعدی")} · {FaNumber(_lesson.TransitionRemaining)} ثانیه {(_lesson.Running?"Ⅱ":"▶")}":$"{FaNumber(_lesson.Remaining)} ثانیه {(_lesson.Running?"Ⅱ":"▶")}";
        DoneButton.Content=_lesson.IsLast?"انجام دادم ✓":"انجام دادم؛ بعدی";
        double progress=100*_lesson.Progress,displayed=LessonProgress.Value;
        LessonProgress.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty,null);
        LessonProgress.Value=progress;
        if(MotionAllowed&&_lesson.Running&&progress>displayed&&progress-displayed<30)
            LessonProgress.BeginAnimation(System.Windows.Controls.Primitives.RangeBase.ValueProperty,new DoubleAnimation(displayed,progress,TimeSpan.FromMilliseconds(350)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.Stop});
        PreviousButton.IsEnabled=_lesson.Index>0;
        NextButton.IsEnabled=_lesson.Index+1<_lesson.Steps.Count;
        NextButton.Content=NextButton.IsEnabled?"حرکت بعدی":"آخرین حرکت";
        CoachBuddy.InvalidateVisual();
    }
    private static string FaNumber(int n)=>string.Concat(n.ToString(System.Globalization.CultureInfo.InvariantCulture).Select(c=>c is >= '0' and <= '9'?(char)('۰'+c-'0'):c));
    private void Next_Click(object sender,RoutedEventArgs e)
    {
        if(_lesson?.Next()==true){_alertAge=0;_lastTick=Environment.TickCount64;CoachBuddy.RestartPose();UpdateLesson();SetMotion();FitHeight();}
    }
    private void Previous_Click(object sender,RoutedEventArgs e)
    {
        if(_lesson?.Previous()==true){_alertAge=0;_lastTick=Environment.TickCount64;CoachBuddy.RestartPose();UpdateLesson();SetMotion();FitHeight();}
    }
    private void Timer_Click(object sender,RoutedEventArgs e)
    {
        if(_lesson==null)return;
        _lesson.Running=!_lesson.Running;
        _alertAge=0;_lastTick=Environment.TickCount64;UpdateLesson();SetMotion();
    }
    private void Repeat_Click(object sender,RoutedEventArgs e)
    {
        if(_lesson==null)return;_lesson.Restart();CoachBuddy.RestartPose();_alertAge=0;_lastTick=Environment.TickCount64;UpdateLesson();SetMotion();
    }
    private void Plus_Click(object sender,RoutedEventArgs e)
    {
        _quick=!_quick;_about=false;_preferences=false;_library=false;Reveal(_active!=null,true,animate:false);
    }
    private void EyeQuick_Click(object sender,RoutedEventArgs e)=>ShowCoach(ReminderKind.Eye,true);
    private void BodyQuick_Click(object sender,RoutedEventArgs e)=>ShowCoach(ReminderKind.Move,true);
    private void About_Click(object sender,RoutedEventArgs e)
    {
        DismissActive();_about=true;_quick=false;_preferences=false;_library=false;Reveal(false,true,animate:false);
    }

    private void Finish(ReminderResponse response,bool hide=true)
    {
        if(_active is not ReminderKind kind)return;
        _engine.Respond(kind,response,_practice);
        if(!_practice)
        {
            _history.Add(new(DateTimeOffset.Now,kind,response));
            if(_history.Count>200)_history.RemoveAt(0);
            try{HistoryStore.Save(_history);}catch(Exception ex){Diagnostics.Record("history",ex);}
        }
        _active=null;if(_lesson!=null)_lesson.Running=false;
        CoachBuddy.State=response==ReminderResponse.Done?BuddyState.Completed:BuddyState.Snoozed;
        _lesson=null;CoachBuddy.Lesson=null;_preferences=false;
        if(hide)Conceal();
    }
    private void DismissActive(){if(_active!=null)Finish(ReminderResponse.Dismissed,hide:false);}
    private void Hide_Click(object sender,RoutedEventArgs e){DismissActive();_preferences=false;Conceal();}
    private void Done_Click(object sender,RoutedEventArgs e)
    {
        if(_lesson is {IsLast:false}){Next_Click(sender,e);return;}
        Finish(ReminderResponse.Done);
    }
    private void Snooze_Click(object sender,RoutedEventArgs e)=>Finish(ReminderResponse.Snooze);
    private void OpenPreferences()
    {
        DismissActive();_preferences=true;_quick=false;_about=false;_library=false;
        StartupToggle.IsChecked=_settings.AutoStart;MotionToggle.IsChecked=_settings.AnimationsEnabled&&!_settings.ReducedMotion;
        EyeToggle.IsChecked=_settings.EyeEnabled;PostureToggle.IsChecked=_settings.PostureEnabled;MoveToggle.IsChecked=_settings.MoveEnabled;WaterToggle.IsChecked=_settings.WaterEnabled;
        EyeMinutesInput.Text=FaNumber(_settings.EyeMinutes);PostureMinutesInput.Text=FaNumber(_settings.PostureMinutes);
        MoveMinutesInput.Text=FaNumber(_settings.MoveMinutes);WaterMinutesInput.Text=FaNumber(_settings.WaterMinutes);
        _draftCustom=_settings.CustomReminders.Select(CloneReminder).ToList();
        PersonalRemindersList.ItemsSource=_draftCustom;SettingsStatus.Text="";
        NewPersonalPanel.Visibility=Visibility.Collapsed;Reveal(false,true);
    }
    private void Preferences_Click(object sender,RoutedEventArgs e)=>OpenPreferences();
    private static CustomReminder CloneReminder(CustomReminder r)=>new(){Id=r.Id,Title=r.Title,Message=r.Message,Minutes=r.Minutes,Enabled=r.Enabled};
    private void NewPersonal_Click(object sender,RoutedEventArgs e)
    {
        NewPersonalPanel.Visibility=NewPersonalPanel.Visibility==Visibility.Visible?Visibility.Collapsed:Visibility.Visible;FitHeight();
        if(NewPersonalPanel.Visibility==Visibility.Visible){BodyScroll.ScrollToEnd();PersonalTitleInput.Focus();}
    }
    private void AddPersonal_Click(object sender,RoutedEventArgs e)
    {
        try
        {
            if(_draftCustom.Count>=20)throw new ArgumentException("حداکثر ۲۰ یادآور شخصی می‌توان افزود.");
            if(string.IsNullOrWhiteSpace(PersonalTitleInput.Text))throw new ArgumentException("عنوان یادآور را بنویس.");
            _draftCustom.Add(new(){Title=PersonalTitleInput.Text.Trim(),Message=PersonalMessageInput.Text.Trim(),Minutes=SettingsValidation.Minutes(PersonalMinutesInput.Text,1440)});
            PersonalRemindersList.ItemsSource=_draftCustom.ToList();PersonalTitleInput.Clear();PersonalMessageInput.Clear();
            NewPersonalPanel.Visibility=Visibility.Collapsed;SettingsStatus.Text="به فهرست اضافه شد؛ برای ثبت، ذخیره را انتخاب کنید.";FitHeight();BodyScroll.ScrollToEnd();
        }
        catch(ArgumentException ex){SettingsStatus.Text=ex.Message;}
    }
    private void RemovePersonal_Click(object sender,RoutedEventArgs e)
    {
        if(sender is Button b&&b.Tag is string id){_draftCustom.RemoveAll(x=>x.Id==id);PersonalRemindersList.ItemsSource=_draftCustom.ToList();SettingsStatus.Text="حذف شد؛ برای ثبت، ذخیره را انتخاب کنید.";FitHeight();}
    }
    private static bool HasInputErrors(DependencyObject root)
    {
        if(Validation.GetHasError(root))return true;
        for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)if(HasInputErrors(VisualTreeHelper.GetChild(root,i)))return true;
        return false;
    }
    private void Save_Click(object sender,RoutedEventArgs e)
    {
        try
        {
            if(HasInputErrors(PersonalRemindersList))throw new ArgumentException("زمان یادآور شخصی باید عددی بین ۱ و ۱۴۴۰ باشد.");
            int eye=SettingsValidation.Minutes(EyeMinutesInput.Text),posture=SettingsValidation.Minutes(PostureMinutesInput.Text),move=SettingsValidation.Minutes(MoveMinutesInput.Text),water=SettingsValidation.Minutes(WaterMinutesInput.Text);
            var candidate=System.Text.Json.JsonSerializer.Deserialize<AppSettings>(System.Text.Json.JsonSerializer.Serialize(_settings))!;
            candidate.CustomReminders=_draftCustom.Select(CloneReminder).ToList();
            candidate.EyeMinutes=eye;candidate.PostureMinutes=posture;candidate.MoveMinutes=move;candidate.WaterMinutes=water;
            candidate.EyeEnabled=EyeToggle.IsChecked==true;candidate.PostureEnabled=PostureToggle.IsChecked==true;candidate.MoveEnabled=MoveToggle.IsChecked==true;
            candidate.AutoStart=StartupToggle.IsChecked==true;candidate.AnimationsEnabled=MotionToggle.IsChecked==true;
            candidate.ReducedMotion=!candidate.AnimationsEnabled;candidate.WaterEnabled=WaterToggle.IsChecked==true;
            SettingsStore.Save(candidate);_settings=candidate;if(!_diagnosticOnly)SettingsStore.EnsureStartup(_settings.AutoStart);_engine.Configure(_settings);_preferences=false;Conceal();
        }
        catch(Exception ex)when(ex is ArgumentException or System.IO.IOException or UnauthorizedAccessException){Diagnostics.Record("settings",ex);SettingsStatus.Text=ex is ArgumentException?ex.Message:"ذخیره نشد؛ دوباره تلاش کنید.";}
    }
    private void Library_Click(object sender,RoutedEventArgs e)=>OpenLibrary();
    private void OpenLibrary()
    {
        DismissActive();_library=true;_preferences=false;_quick=false;_about=false;Reveal(false,true);
    }
    private void ExerciseFilter_Changed(object sender,SelectionChangedEventArgs e)
    {
        if(ExercisesList==null)return;
        var group=(ExerciseFilter.SelectedItem as ListBoxItem)?.Content?.ToString()??"همه";
        ExercisesList.ItemsSource=ExerciseCatalog.All.Where(x=>group=="همه"||x.Group==group).ToList();
        if(IsLoaded&&_library){BodyScroll.ScrollToTop();FitHeight();}
    }
    private void Exercise_Click(object sender,RoutedEventArgs e)
    {
        if(sender is Button b&&b.Tag is ExerciseMotion motion)ShowExercise(motion);
    }
    private void ShowExercise(ExerciseMotion motion)
    {
        ShowCoach(ReminderKind.Move,true);
        var group=(ExerciseFilter.SelectedItem as ListBoxItem)?.Content?.ToString()??"همه";
        var library=ExerciseCatalog.All.Where(x=>group=="همه"||x.Group==group).ToList();
        _lesson=new ExerciseLesson(ReminderKind.Move,library,library.FindIndex(x=>x.Motion==motion)){Running=true};
        CoachBuddy.Lesson=_lesson;_promptText="راهنمای تمرین";UpdateLesson();Reveal(true,true,animate:false);
    }
    private void ShowPersonal(CustomReminder custom,bool practice=false)
    {
        if(!practice&&(_active!=null||_preferences||_library||_about)){_engine.Respond(ReminderKind.Custom,ReminderResponse.Snooze);return;}
        if(_active!=null)DismissActive();
        _active=ReminderKind.Custom;_practice=practice;_lesson=null;CoachBuddy.Lesson=null;
        _preferences=false;_quick=false;_about=false;_library=false;_alertAge=0;_lastTick=Environment.TickCount64;
        _promptText=custom.Title;PersonalMessageText.Text=string.IsNullOrWhiteSpace(custom.Message)?custom.Title:custom.Message;
        PromptArt.Visibility=Visibility.Collapsed;PromptDoneButton.Content="انجام شد ✓";PromptTimeout.Text="جمع‌شدن خودکار تا ۲۰ ثانیه";Reveal(false,practice);
    }
    private void Details_Changed(object sender,RoutedEventArgs e)
    {
        if(IsLoaded)Dispatcher.BeginInvoke((Action)FitHeight);
    }
    private void Workstation_Changed(object sender,RoutedEventArgs e)
    {
        if(WorkstationStatus==null)return;
        int count=new[]{CheckMonitor,CheckBack,CheckFeet,CheckWrists,CheckLight}.Count(x=>x?.IsChecked==true);
        WorkstationStatus.Text=$"{FaNumber(count)} از ۵ مورد بررسی شد";
    }
    private void ExitApp()
    {
        if(_closing)return;_closing=true;
        _tick.Stop();_pointer.Stop();_engine.Dispose();
        CoachBuddy.Animate=false;CoachBuddy.Refresh();
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged-=DisplayChanged;
        _edgeHint?.Close();_tray?.Dispose();_trayIcon?.Dispose();Application.Current.Shutdown();
    }
    protected override void OnClosing(CancelEventArgs e)
    {
        if(!_closing&&!_diagnosticOnly){e.Cancel=true;Hide_Click(this,new RoutedEventArgs());return;}
        base.OnClosing(e);
    }
    public void RunUiSelfTest()
    {
        _settings.ReducedMotion=true;
        if(FontFamily.Source.IndexOf("IRANYekanX",StringComparison.Ordinal)<0)throw new InvalidOperationException("Yekan resource not applied.");
        var font=new Typeface(FontFamily,FontStyles.Normal,FontWeights.Normal,FontStretches.Normal);
        if(!font.TryGetGlyphTypeface(out var glyph)||!glyph.CharacterToGlyphMap.ContainsKey(0x06CC))throw new InvalidOperationException("Embedded Persian font missing.");
        void Capture(string name)
        {
            BeginAnimation(LeftProperty,null);BeginAnimation(OpacityProperty,null);Opacity=1;Left=_anchorLeft;Top=_anchorTop;
            FitHeight();UpdateLayout();
            var b=new System.Windows.Media.Imaging.RenderTargetBitmap((int)Math.Ceiling(ActualWidth),(int)Math.Ceiling(ActualHeight),96,96,PixelFormats.Pbgra32);b.Render(this);
            var path=Environment.GetEnvironmentVariable("ERGONOMY_UI_OUTPUT");
            if(!string.IsNullOrEmpty(path)){System.IO.Directory.CreateDirectory(path);var enc=new System.Windows.Media.Imaging.PngBitmapEncoder();enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(b));using var f=System.IO.File.Create(System.IO.Path.Combine(path,name+".png"));enc.Save(f);}
        }
        Reveal(false,animate:false);Capture("hover-pill");
        if(Width>320||Height>90)throw new InvalidOperationException("Hover pill too large.");
        Plus_Click(this,new RoutedEventArgs());Capture("quick-actions");
        if(QuickActions.Children.Count!=3)throw new InvalidOperationException("Quick menu must have exactly three buttons.");
        About_Click(this,new RoutedEventArgs());Capture("about");
        var phoneBottom=AuthorPhone.TranslatePoint(new System.Windows.Point(0,AuthorPhone.ActualHeight),this).Y;
        if(AuthorName.Text!="میرجعفری"||AuthorPhone.Text!="۰۹۱۸۶۴۴۱۸۰۱"||phoneBottom>ActualHeight-8||AuthorPhone.ActualHeight<=0)
            throw new InvalidOperationException("About attribution is clipped or missing.");
        _about=false;
        ShowCoach(ReminderKind.Posture,true);Capture("posture-reminder");
        if(_lesson!=null||PersonalPanel.Visibility!=Visibility.Visible||PromptArt.Kind!=ReminderKind.Posture)throw new InvalidOperationException("Posture self-check was presented as an exercise.");
        Finish(ReminderResponse.Done,hide:false);
        ShowCoach(ReminderKind.Water,true);Capture("water-glass");
        if(_lesson!=null||PromptArt.Kind!=ReminderKind.Water||PromptArt.Visibility!=Visibility.Visible)throw new InvalidOperationException("Water glass reminder missing.");
        Finish(ReminderResponse.Done,hide:false);
        foreach(var kind in new[]{ReminderKind.Eye,ReminderKind.Move})
        {
            ShowCoach(kind,true);
            for(int i=0;i<_lesson!.Steps.Count;i++)
            {
                UpdateLesson();Capture(kind+"-step-"+_lesson.Index);
                double remaining=_lesson.Current.Seconds-_lesson.Elapsed;
                while(remaining>0){var delta=Math.Min(1,remaining);_lesson.Advance(delta);remaining-=delta;}
                if(!_lesson.StepComplete)throw new InvalidOperationException("Independent step timer did not finish.");
                if(i+1<_lesson.Steps.Count){
                    if(_lesson.Index!=i)throw new InvalidOperationException("Timer auto-advanced without user input.");
                    Next_Click(this,new RoutedEventArgs());
                }
            }
            if(!_lesson.Finished)throw new InvalidOperationException("Lesson did not finish.");
            UpdateLesson();Capture(kind+"-complete");Finish(ReminderResponse.Done,hide:false);
        }
        ShowCoach(ReminderKind.Move,true);Next_Click(this,new RoutedEventArgs());Previous_Click(this,new RoutedEventArgs());
        if(_lesson!.Index!=0||_lesson.Elapsed!=0)throw new InvalidOperationException("Previous did not reset selected timer.");
        Timer_Click(this,new RoutedEventArgs());
        if(_lesson.Running)throw new InvalidOperationException("Pause did not stop timer.");
        Timer_Click(this,new RoutedEventArgs());Finish(ReminderResponse.Done,hide:false);
        ShowCoach(ReminderKind.Eye,true);
        for(int i=0;i<5;i++)AdvanceVisibleSession(1);
        Capture("auto-next-countdown");
        if(_lesson is not {Index:0,StepComplete:true,TransitionRemaining:4})throw new InvalidOperationException("Four-second transition did not start.");
        Timer_Click(this,new RoutedEventArgs());AdvanceVisibleSession(3);
        if(_lesson.TransitionRemaining!=4)throw new InvalidOperationException("Paused transition did not freeze.");
        Timer_Click(this,new RoutedEventArgs());
        for(int i=0;i<3;i++)AdvanceVisibleSession(1);
        if(_lesson.Index!=0)throw new InvalidOperationException("Automatic transition happened early.");
        AdvanceVisibleSession(1);
        if(_lesson.Index!=1||_lesson.Remaining!=20)throw new InvalidOperationException("Next exercise did not get a fresh timer.");
        for(int i=0;i<20;i++)AdvanceVisibleSession(1);Capture("final-countdown");
        for(int i=0;i<4;i++)AdvanceVisibleSession(1);
        if(_active==null)throw new InvalidOperationException("Final card closed before five seconds.");
        AdvanceVisibleSession(1);
        if(_active!=null)throw new InvalidOperationException("Final automatic timeout did not close the session.");
        ShowCoach(ReminderKind.Eye,true);Done_Click(this,new RoutedEventArgs());
        if(_lesson?.Index!=1)throw new InvalidOperationException("Per-step Done did not advance.");
        Done_Click(this,new RoutedEventArgs());
        if(_active!=null)throw new InvalidOperationException("Final explicit Done did not close.");
        int historyBefore=_history.Count;ShowCoach(ReminderKind.Water);
        for(int i=0;i<20;i++)AdvanceVisibleSession(1);
        if(_active!=null||_history.Count!=historyBefore+1||_history.Last().Response!=ReminderResponse.TimedOut)throw new InvalidOperationException("Timeout falsely recorded completion or failed to close.");
        OpenLibrary();Capture("exercise-library");
        int VisibleScrollbars(DependencyObject root)
        {
            int count=root is System.Windows.Controls.Primitives.ScrollBar {IsVisible:true}?1:0;
            for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)count+=VisibleScrollbars(VisualTreeHelper.GetChild(root,i));
            return count;
        }
        if(VisibleScrollbars(BodyScroll)!=1)throw new InvalidOperationException("Library should have one scroll surface.");
        if(ExercisesList.Items.Count!=20)throw new InvalidOperationException("Movement catalog is incomplete.");
        if(ExerciseFilter.Items.Count!=7)throw new InvalidOperationException("Movement filter chips missing.");
        ExerciseFilter.SelectedIndex=1;
        if(ExercisesList.Items.Count!=2)throw new InvalidOperationException("Eye movement filter did not apply.");
        Capture("filter-eye");
        ExerciseFilter.SelectedIndex=0;
        WorkstationDetails.IsExpanded=true;CheckMonitor.IsChecked=CheckBack.IsChecked=CheckFeet.IsChecked=CheckWrists.IsChecked=CheckLight.IsChecked=true;
        if(WorkstationStatus.Text!="۵ از ۵ مورد بررسی شد")throw new InvalidOperationException("Workstation self-check tally failed.");
        Capture("workstation-check");CheckFeet.IsChecked=false;
        if(WorkstationStatus.Text!="۴ از ۵ مورد بررسی شد")throw new InvalidOperationException("Workstation unchecked tally failed.");
        WorkstationDetails.IsExpanded=false;
        foreach(var exercise in ExerciseCatalog.All)
        {
            ShowExercise(exercise.Motion);Capture("catalog-"+exercise.Motion);Finish(ReminderResponse.Done,hide:false);
        }
        OpenPreferences();Capture("preferences");
        int originalCount=_settings.CustomReminders.Count;
        EyeMinutesInput.Text="۲۲";PersonalTitleInput.Text="یادآور آزمایشی";PersonalMessageInput.Text="وقت تغییر فعالیت است";PersonalMinutesInput.Text="۷";
        AddPersonal_Click(this,new RoutedEventArgs());
        if(_draftCustom.Count!=originalCount+1||_settings.CustomReminders.Count!=originalCount)throw new InvalidOperationException("Reminder draft changed live settings before Save.");
        BodyScroll.ScrollToEnd();Capture("preferences-personal");
        Save_Click(this,new RoutedEventArgs());
        var restored=SettingsStore.Load();
        if(restored.EyeMinutes!=22||restored.CustomReminders.Count!=originalCount+1)throw new InvalidOperationException("Timer/personal reminder persistence failed.");
        OpenPreferences();EyeMinutesInput.Text="۰";Save_Click(this,new RoutedEventArgs());
        if(_settings.EyeMinutes!=22||string.IsNullOrEmpty(SettingsStatus.Text))throw new InvalidOperationException("Invalid interval was not rejected.");
        EyeMinutesInput.Text="۲۲";
        var testId=_draftCustom.Last().Id;RemovePersonal_Click(new Button{Tag=testId},new RoutedEventArgs());Save_Click(this,new RoutedEventArgs());
        if(SettingsStore.Load().CustomReminders.Count!=originalCount)throw new InvalidOperationException("Personal reminder removal did not persist.");
        ShowPersonal(new(){Title="یادآور شخصی",Message="وقت کمی تغییر فعالیت است",Minutes=7},true);Capture("personal-reminder");Finish(ReminderResponse.Done,hide:false);
        _preferences=false;
        Conceal(immediate:true);
        if(IsVisible)throw new InvalidOperationException("Concealed window still visible.");
        WindowsEnvironment.TestCornerPointer(this,()=>{
            PointerTick(this,EventArgs.Empty);PointerTick(this,EventArgs.Empty);
            if(!IsVisible||!_shown)throw new InvalidOperationException("Real right-edge pointer reveal failed.");
        });
        _reveal.Hide();Conceal(immediate:true);
        _edgeHint=new EdgeHandleWindow();UpdateEdgeHint();_edgeHint.UpdateLayout();
        if(!_edgeHint.IsVisible)throw new InvalidOperationException("Discoverability handle missing.");
        var hintBitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(16,56,96,96,PixelFormats.Pbgra32);hintBitmap.Render(_edgeHint);
        var hintOutput=Environment.GetEnvironmentVariable("ERGONOMY_UI_OUTPUT");
        if(!string.IsNullOrEmpty(hintOutput)){
            var enc=new System.Windows.Media.Imaging.PngBitmapEncoder();enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(hintBitmap));using var file=System.IO.File.Create(System.IO.Path.Combine(hintOutput,"edge-hint.png"));enc.Save(file);
        }
        _quiet=true;UpdateEdgeHint();if(_edgeHint.IsVisible)throw new InvalidOperationException("Handle did not respect quiet mode.");_quiet=false;UpdateEdgeHint();
        using(var iconStream=Application.GetResourceStream(new Uri("pack://application:,,,/Assets/AppIcon.ico"))!.Stream)
        using(var icon=new System.Drawing.Icon(iconStream))if(icon.Width<16)throw new InvalidOperationException("Tray icon resource invalid.");
        void Pump(int milliseconds)
        {
            var frame=new DispatcherFrame();
            var stop=new DispatcherTimer {Interval=TimeSpan.FromMilliseconds(milliseconds)};
            stop.Tick+=(_,_)=>{stop.Stop();frame.Continue=false;};
            stop.Start();Dispatcher.PushFrame(frame);
        }
        ShowCoach(ReminderKind.Move,true);
        Next_Click(this,new RoutedEventArgs());
        _settings.ReducedMotion=false;_settings.AnimationsEnabled=true;_quiet=false;
        SetMotion();
        Capture("motion-walk-before");double before=CoachBuddy.AnimationClock;
        Pump(420);Capture("motion-walk-after");
        Diagnostics.Record("motion-test",$"SystemAnimation={SystemParameters.ClientAreaAnimation};Visible={CoachBuddy.IsVisible};Reduced={CoachBuddy.ReducedMotion};Animate={CoachBuddy.Animate};ClockBefore={before};ClockAfter={CoachBuddy.AnimationClock}");
        if(SystemParameters.ClientAreaAnimation)
        {
            if(CoachBuddy.AnimationClock<=before+.15)throw new InvalidOperationException("Instructional animation clock did not run.");
        }
        else Diagnostics.Record("motion-test","SKIP live motion: operating system has disabled client-area animations; reduced-motion behavior retained.");
        if(SystemParameters.ClientAreaAnimation)
        {
            ShowExercise(ExerciseMotion.NeckLeft);SetMotion();Capture("neck-neutral");Pump(2400);Capture("neck-hold");
            if(CoachBuddy.AnimationClock<2)throw new InvalidOperationException("Held neck coaching did not animate.");
        }
        ShowCoach(ReminderKind.Eye,true);
        var shortSteps=ExerciseCatalog.Routine(ReminderKind.Eye).Select(x=>x with{Seconds=1}).ToList();
        _lesson=new ExerciseLesson(ReminderKind.Eye,shortSteps){Running=true};CoachBuddy.Lesson=_lesson;
        _lastTick=Environment.TickCount64;_tick.Start();
        try{Pump(6200);}finally{_tick.Stop();}
        if(_lesson?.Index!=1)throw new InvalidOperationException("Real dispatcher timer did not advance after the four-second grace.");
        _lesson=new ExerciseLesson(ReminderKind.Eye,[shortSteps[1]]){Running=true};CoachBuddy.Lesson=_lesson;_lastTick=Environment.TickCount64;_tick.Start();
        try{Pump(6500);}finally{_tick.Stop();}
        if(_active!=null)throw new InvalidOperationException("Real dispatcher timer did not close the last card after five seconds.");
        Diagnostics.Record("timed-session-test","PASS: actual dispatcher timers exercised 4-second next and 5-second final closure; no automatic user confirmation.");
        Finish(ReminderResponse.Dismissed,hide:false);Conceal(immediate:true);Reveal(false,animate:true);
        if(SystemParameters.ClientAreaAnimation){
            if(Opacity>.1)throw new InvalidOperationException("Entry flashed at full opacity.");
            Pump(240);if(Opacity<=0||Opacity>=1)throw new InvalidOperationException("Gentle fade did not run progressively.");
            Pump(600);if(Opacity<.99)throw new InvalidOperationException("Gentle fade did not finish.");
        }
        Conceal(immediate:true);double stopped=CoachBuddy.AnimationClock;
        Pump(250);
        if(CoachBuddy.AnimationClock!=stopped)throw new InvalidOperationException("Hidden character still animating.");
        if(_lesson!=null)_lesson.Running=false;
        _edgeHint?.Close();_edgeHint=null;_engine.Dispose();
    }
}
internal static class BrushHelpers
{
    public static Brush ToBrush(this string hex)=>(Brush)new BrushConverter().ConvertFromString(hex)!;
}
