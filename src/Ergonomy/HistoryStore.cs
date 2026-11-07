using System.IO;
using System.Text.Json;

namespace Ergonomy;
public sealed record HistoryEntry(DateTimeOffset Time, ReminderKind Kind, ReminderResponse Response)
{
    public override string ToString()
    {
        var kind = Kind switch { ReminderKind.Eye => "چشم", ReminderKind.Posture => "نشستن", ReminderKind.Move => "حرکت", ReminderKind.Custom=>"یادآور شخصی", _ => "آب" };
        var action = Response switch { ReminderResponse.Done => "انجام شد", ReminderResponse.Snooze => "تعویق", _ => "رد یا پایان زمان" };
        return $"{Time.ToLocalTime():MM/dd HH:mm} · {kind} · {action}";
    }
}
public static class HistoryStore
{
    private static readonly string FilePath = Path.Combine(LocalData.Root,"history.json");
    public static List<HistoryEntry> Load()
    {
        try {return (JsonSerializer.Deserialize<List<HistoryEntry>>(File.ReadAllText(FilePath)) ?? []).TakeLast(200).ToList();}
        catch(Exception ex) when(ex is IOException or UnauthorizedAccessException or JsonException) {return [];}
    }
    public static void Save(List<HistoryEntry> items)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath+".tmp",JsonSerializer.Serialize(items.TakeLast(200)));
        File.Move(FilePath+".tmp",FilePath,true);
    }
}
