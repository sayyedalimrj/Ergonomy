using Ergonomy;
int passed = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception("FAIL: " + name); Console.WriteLine("PASS: " + name); passed++; }
void Run(WellnessSchedule s, int seconds, bool idle=false, bool quiet=false) {for(int i=0;i<seconds;i++) s.Advance(1,idle,quiet);}
var defaults = new AppSettings();
Check(defaults.EyeMinutes==20 && defaults.PostureMinutes==30 && defaults.MoveMinutes==55 && !defaults.WaterEnabled && !defaults.AutoStart,"scientific product defaults and optional startup");
var s = new WellnessSchedule(defaults);
Run(s,1199); Check(s.Pending==null,"no early eye reminder");
s.Advance(1,false,false); Check(s.Pending==ReminderKind.Eye,"eye due at 1200 active seconds");
Run(s,100); Check(s.Pending==ReminderKind.Eye,"single pending reminder");
s.Respond(ReminderKind.Eye,ReminderResponse.Done); Check(s.Pending==null && s.Next!.Value.Remaining.TotalSeconds>0,"Done clears pending");
var idleSchedule=new WellnessSchedule(defaults); Run(idleSchedule,3600,idle:true); Check(idleSchedule.ActiveSeconds==0 && idleSchedule.Pending==null,"idle and lock do not charge work");
idleSchedule.Advance(7200,false,false); Check(idleSchedule.ActiveSeconds==0,"sleep gap does not charge work");
var quietSchedule=new WellnessSchedule(defaults); Run(quietSchedule,1300,quiet:true); Check(quietSchedule.Pending==null,"fullscreen suppresses delivery while counting active work");
quietSchedule.Advance(1,false,false); Check(quietSchedule.Pending==ReminderKind.Eye,"quiet exit delivers one overdue reminder");
quietSchedule.Respond(ReminderKind.Eye,ReminderResponse.Done); Run(quietSchedule,88); Check(quietSchedule.Pending==null,"cooldown prevents catch-up burst");
var snooze = new WellnessSchedule(new(){EyeMinutes=1,PostureEnabled=false,MoveEnabled=false}); Run(snooze,60); snooze.Respond(ReminderKind.Eye,ReminderResponse.Snooze); Run(snooze,599); Check(snooze.Pending==null,"snooze holds target 10 minutes"); snooze.Advance(1,false,false); Check(snooze.Pending==ReminderKind.Eye,"snooze reoffers target");
var practice=new WellnessSchedule(defaults);Run(practice,1199);practice.Respond(ReminderKind.Eye,ReminderResponse.Done,practice:true);practice.Advance(1,false,false);Check(practice.Pending==ReminderKind.Eye,"manual practice does not reset automatic schedule");
var pause=new WellnessSchedule(defaults);pause.PauseFor(TimeSpan.FromSeconds(120));Run(pause,119);Check(pause.ActiveSeconds==0,"manual pause freezes work");pause.Resume();pause.Advance(1,false,false);Check(pause.ActiveSeconds==1,"manual resume continues");
var disabled=new WellnessSchedule(new(){EyeEnabled=false,PostureEnabled=false,MoveEnabled=false});Run(disabled,10000);Check(disabled.Next==null && disabled.Pending==null,"all reminders disabled");
var hydration=new WellnessSchedule(new(){EyeEnabled=false,PostureEnabled=false,MoveEnabled=false,WaterEnabled=true});Run(hydration,7200);Check(hydration.Pending==ReminderKind.Water,"optional hydration at 120 active minutes");
Console.WriteLine($"{passed} behavioral assertions passed.");
