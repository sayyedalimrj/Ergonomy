using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace Ergonomy;


public static class SettingsStore
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Ergonomy", "settings.json");
    private const string StartupKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
                if (s is not null)
                {
                    s.EyeMinutes = Math.Clamp(s.EyeMinutes, 1, 480);
                    s.PostureMinutes = Math.Clamp(s.PostureMinutes, 1, 480);
                    s.MoveMinutes = Math.Clamp(s.MoveMinutes, 1, 480);
                    s.WaterMinutes = Math.Clamp(s.WaterMinutes, 1, 480);
                    if (!Enum.IsDefined(s.Buddy)) s.Buddy = BuddyKind.Posture;
                    return s;
                }
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        { Diagnostics.Record("settings-load", ex); }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        var temp = SettingsPath + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, SettingsPath, true);
    }

    public static void EnsureStartup(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(StartupKey);
            if (key is null) return;
            if (enabled)
            {
                var executable = Environment.ProcessPath;
                if (!string.IsNullOrWhiteSpace(executable) &&
                    executable.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    key.SetValue("Ergonomy", "\"" + executable + "\"");
                }
            }
            else key.DeleteValue("Ergonomy", false);
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or UnauthorizedAccessException or IOException)
        { Diagnostics.Record("startup-registry", ex); }
    }
}
