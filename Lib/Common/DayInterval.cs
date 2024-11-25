namespace PodereBot.Lib.Common;

public class DayInterval()
{
    public required int HoursFrom { get; init; }
    public required int MinutesFrom { get; init; }
    public required int HoursTo { get; init; }
    public required int MinutesTo { get; init; }
    public int FromTimestamp => HoursFrom * 3600 + MinutesFrom * 60;
    public int ToTimestamp => HoursTo * 3600 + MinutesTo * 60;

    public override string ToString()
    {
        return $"Dalle {HoursFrom:D2}:{MinutesFrom:D2} alle {HoursTo:D2}:{MinutesTo:D2}";
    }
}
