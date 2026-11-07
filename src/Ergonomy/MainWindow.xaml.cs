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
        ExercisesList.ItemsSource=ExerciseCatalog.All.Where(x=>x.Motion!=ExerciseMotion.Water);
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
        _transition++;
        _shown=true;_reveal.Reveal();UpdateEdgeHint();BodyScroll.ScrollToTop();
        LessonPanel.Visibility=lesson&&_lesson!=null&&!_quick&&!_about&&!_preferences&&!_library?Visibility.Visible:Visibility.Collapsed;
        PersonalPanel.Visibility=_active==ReminderKind.Custom&&!_quick&&!_about&&!_preferences&&!_library?Visibility.Visible:Visibility.Collapsed;
        LibraryPanel.Visibility=_library?Visibility.Visible:Visibility.Collapsed;
        QuickActions.Visibility=_quick?Visibility.Visible:Visibility.Collapsed;
        AboutPanel.Visibility=_about?Visibility.Visible:Visibility.Collapsed;
        PreferencesPanel.Visibility=_preferences?Visibility.Visible:Visibility.Collapsed;
        PlusButton.Content=_quick?"×":"+";
        FitHeight();PositionCorner();
        WindowsEnvironment.SetInteractive(this,interactive);
        if(!IsVisible)Show();
        SetMotion();UpdateNext();
        BeginAnimation(LeftProperty,null);BeginAnimation(OpacityProperty,null);
        Opacity=1;
        if(animate && MotionAllowed)
        {
            BeginAnimation(LeftProperty,new DoubleAnimation(_anchorLeft+Width+16,_anchorLeft,TimeSpan.FromMilliseconds(280)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseOut},FillBehavior=FillBehavior.Stop});
            BeginAnimation(OpacityProperty,new DoubleAnimation(0,1,TimeSpan.FromMilliseconds(220)){FillBehavior=FillBehavior.Stop});
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
        _transition++;int transition=_transition;
        _shown=false;_reveal.Hide();_quick=false;_about=false;_library=false;
        QuickActions.Visibility=Visibility.Collapsed;AboutPanel.Visibility=Visibility.Collapsed;PlusButton.Content="+";
        CoachBuddy.Animate=DockBuddy.Animate=false;CoachBuddy.Refresh();DockBuddy.Refresh();
        BeginAnimation(LeftProperty,null);BeginAnimation(OpacityProperty,null);
        if(immediate||!MotionAllowed||!IsVisible){Hide();Left=_anchorLeft;UpdateEdgeHint();return;}
        var slide=new DoubleAnimation(Left,_anchorLeft+Width+16,TimeSpan.FromMilliseconds(220)){EasingFunction=new CubicEase{EasingMode=EasingMode.EaseIn},FillBehavior=FillBehavior.Stop};
        slide.Completed+=(_,_)=>{if(transition==_transition){Hide();Left=_anchorLeft;UpdateEdgeHint();}};
        BeginAnimation(LeftProperty,slide);
        BeginAnimation(OpacityProperty,new DoubleAnimation(1,0,TimeSpan.FromMilliseconds(180)){FillBehavior=FillBehavior.Stop});
    }
    private bool MotionAllowed=>_settings.AnimationsEnabled&&!_settings.ReducedMotion&&SystemParameters.ClientAreaAnimation;
    private void SetMotion()
    {
        DockBuddy.Kind=_settings.Buddy;
        DockBuddy.Animate=false; // no decorative motion while waiting
        CoachBuddy.ReducedMotion=_settings.ReducedMotion;
        CoachBuddy.Animate=_shown&&!_quick&&!_about&&!_preferences&&!_library&&MotionAllowed&&!_quiet&&(_diagnosticOnly||!_engine.IsIdle)&&(_lesson?.Running??false);
        DockBuddy.Refresh();CoachBuddy.Refresh();
    }
    private void Tick(object? sender,EventArgs e)
    {
        long now=Environment.TickCount64;
        double elapsed=(now-_lastTick)/1000.0;_lastTick=now;
        if(elapsed>3)elapsed=0;
        _quiet=_engine.IsQuiet;
        if(_active!=null&&!_quiet&&!_engine.IsIdle&&!_quick&&!_about&&!_preferences&&!_library)
        {
            _alertAge+=elapsed;
            _lesson?.Advance(elapsed);
            if(_alertAge>=180&&!IsMouseOver&&!IsKeyboardFocusWithin){Finish(ReminderResponse.Dismissed);return;}
            UpdateLesson();
        }
        SetMotion();UpdateNext();
    }
    private void UpdateNext()
    {
        if(_active!=null){TitleText.Text=_promptText;NextText.Text="همراه تو، بدون عجله";return;}
        TitleText.Text="همیار سلامت";
        if(_engine.IsPaused){NextText.Text="فعلاً استراحت";return;}
        var next=_engine.NextReminder;
        NextText.Text=next is null?"همراه تو":$"{_engine.NextTitle} · {FaNumber(Math.Max(1,(int)Math.Ceiling(next.Value.Remaining.TotalMinutes)))} دقیقهٔ دیگر";
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
            ReminderKind.Eye=>"چشم‌ها خسته نشده؟",
            ReminderKind.Posture=>new[]{"درست نشستی؟","گردنت راحت است؟","شانه‌ها رهاست؟"}[_posturePrompt++%3],
            ReminderKind.Move=>"وقت کمی حرکت",_=>"آب خوردی؟"
        };
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
        StepHint.Text=_lesson.StepComplete?(_lesson.Finished?(_lesson.Kind==ReminderKind.Move?"اگر می‌توانی، ۵ تا ۱۰ دقیقه تغییر فعالیت بده":"اگر انجامش دادی، تأیید کن"):"آماده‌ای؟ حرکت بعدی را بزن"):_lesson.Current.Hint;
        StepNumber.Text=$"{_lesson.Current.Group} · {FaNumber(_lesson.Index+1)} از {FaNumber(_lesson.Steps.Count)}";
        StepTitle.ToolTip="راهنمای حرکت: "+_lesson.Current.Source;
        Countdown.Text=_lesson.StepComplete?"↻ تکرار":$"{FaNumber(_lesson.Remaining)} ثانیه {(_lesson.Running?"Ⅱ":"▶")}";
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
        if(_lesson.StepComplete){_lesson.Restart();CoachBuddy.RestartPose();}
        else _lesson.Running=!_lesson.Running;
        _alertAge=0;_lastTick=Environment.TickCount64;UpdateLesson();SetMotion();
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
    private void Done_Click(object sender,RoutedEventArgs e)=>Finish(ReminderResponse.Done);
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
            NewPersonalPanel.Visibility=Visibility.Collapsed;SettingsStatus.Text="به فهرست اضافه شد؛ برای ثبت، ذخیره را بزن.";FitHeight();BodyScroll.ScrollToEnd();
        }
        catch(ArgumentException ex){SettingsStatus.Text=ex.Message;}
    }
    private void RemovePersonal_Click(object sender,RoutedEventArgs e)
    {
        if(sender is Button b&&b.Tag is string id){_draftCustom.RemoveAll(x=>x.Id==id);PersonalRemindersList.ItemsSource=_draftCustom.ToList();SettingsStatus.Text="حذف شد؛ برای ثبت، ذخیره را بزن.";FitHeight();}
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
        catch(Exception ex)when(ex is ArgumentException or System.IO.IOException or UnauthorizedAccessException){Diagnostics.Record("settings",ex);SettingsStatus.Text=ex is ArgumentException?ex.Message:"ذخیره نشد؛ دوباره امتحان کن.";}
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
        ExercisesList.ItemsSource=ExerciseCatalog.All.Where(x=>x.Motion!=ExerciseMotion.Water&&(group=="همه"||x.Group==group)).ToList();
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
        var library=ExerciseCatalog.All.Where(x=>x.Motion!=ExerciseMotion.Water&&(group=="همه"||x.Group==group)).ToList();
        _lesson=new ExerciseLesson(ReminderKind.Move,library,library.FindIndex(x=>x.Motion==motion)){Running=true};
        CoachBuddy.Lesson=_lesson;_promptText="تمرین آرام";UpdateLesson();Reveal(true,true,animate:false);
    }
    private void ShowPersonal(CustomReminder custom,bool practice=false)
    {
        if(!practice&&(_active!=null||_preferences||_library||_about)){_engine.Respond(ReminderKind.Custom,ReminderResponse.Snooze);return;}
        if(_active!=null)DismissActive();
        _active=ReminderKind.Custom;_practice=practice;_lesson=null;CoachBuddy.Lesson=null;
        _preferences=false;_quick=false;_about=false;_library=false;_alertAge=0;_lastTick=Environment.TickCount64;
        _promptText=custom.Title;PersonalMessageText.Text=string.IsNullOrWhiteSpace(custom.Message)?custom.Title:custom.Message;Reveal(false,practice);
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
        foreach(var kind in new[]{ReminderKind.Eye,ReminderKind.Posture,ReminderKind.Move,ReminderKind.Water})
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
        OpenLibrary();Capture("exercise-library");
        int VisibleScrollbars(DependencyObject root)
        {
            int count=root is System.Windows.Controls.Primitives.ScrollBar {IsVisible:true}?1:0;
            for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)count+=VisibleScrollbars(VisualTreeHelper.GetChild(root,i));
            return count;
        }
        if(VisibleScrollbars(BodyScroll)!=1)throw new InvalidOperationException("Library should have one scroll surface.");
        if(ExercisesList.Items.Count!=19)throw new InvalidOperationException("Movement catalog is incomplete.");
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
        foreach(var exercise in ExerciseCatalog.All.Where(x=>x.Motion!=ExerciseMotion.Water))
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
