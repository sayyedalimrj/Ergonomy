using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace Ergonomy;

public sealed class AppSettings
{
    public bool EyeEnabled { get; set; } = true;
    public bool PostureEnabled { get; set; } = true;
    public bool MoveEnabled { get; set; } = true;
    public bool WaterEnabled { get; set; }
    public int EyeMinutes { get; set; } = 20;
    public int PostureMinutes { get; set; } = 30;
    public int MoveMinutes { get; set; } = 55;
    public int WaterMinutes { get; set; } = 120;
    public bool AutoStart { get; set; }
}

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
                var result = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
                if (result != null)
                {
                    result.EyeMinutes = Math.Clamp(result.EyeMinutes, 1, 480);
                    result.PostureMinutes = Math.Clamp(result.PostureMinutes, 1, 480);
                    result.MoveMinutes = Math.Clamp(result.MoveMinutes, 1, 480);
                    result.WaterMinutes = Math.Clamp(result.WaterMinutes, 1, 480);
                    return result;
                }
            }
        }
        catch (IOException) { }
        catch (JsonException) { }
        catch (UnauthorizedAccessException) { }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions
        {
            WriteIndented = true
        }));
    }

    public static void ConfigureStartup(bool enable)
    {
        using var key = Registry.CurrentUser.CreateSubKey(StartupKey);
        if (enable)
        {
            var executable = Environment.ProcessPath ?? throw new InvalidOperationException("مسیر برنامه مشخص نیست.");
            key?.SetValue("Ergonomy", "\"" + executable + "\"");
        }
        else
        {
            key?.DeleteValue("Ergonomy", false);
        }
    }
}
