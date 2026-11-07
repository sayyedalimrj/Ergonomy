namespace Ergonomy;

public sealed class AppSettings
{
    public bool EyeEnabled { get; set; } = true;
    public bool PostureEnabled { get; set; } = true;
    public bool MoveEnabled { get; set; } = true;
    public bool WaterEnabled { get; set; } = false;
    public int EyeMinutes { get; set; } = 20;
    public int PostureMinutes { get; set; } = 30;
    public int MoveMinutes { get; set; } = 55;
    public int WaterMinutes { get; set; } = 120;
    public bool AutoStart { get; set; }
    public bool ReducedMotion { get; set; }
    public bool AnimationsEnabled { get; set; } = true;
    public BuddyKind Buddy { get; set; }
    public double? DockLeft { get; set; }
    public double? DockTop { get; set; }
}
