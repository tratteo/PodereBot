namespace PodereBot.Lib.Common;

public class HeatingProgram()
{
    public List<HeatingInterval> Intervals { get; private set; } = [];

    private HeatingProgram(IEnumerable<HeatingInterval> intervals)
        : this()
    {
        Intervals = intervals.ToList();
    }

    public override string ToString()
    {
        return string.Join("\n", Intervals);
    }

    public string ToCodeString()
    {
        return string.Join("/", Intervals.ConvertAll(i => i.ToCodeString()));
    }

    public HeatingInterval? GetFirstIntervalInProgram()
    {
        var date = DateTime.Now;
        var dayTimestamp = date.Hour * 3600 + date.Minute * 60;
        return Intervals.MinBy(i => i.FromTimestamp - dayTimestamp);
    }

    public bool IsScheduledActive(out HeatingInterval? interval)
    {
        var date = DateTime.Now;
        var dayTimestamp = date.Hour * 3600 + date.Minute * 60;
        interval = Intervals.FirstOrDefault(i => i.FromTimestamp <= dayTimestamp && i.ToTimestamp > dayTimestamp);
        return interval != null;
    }

    public static bool TryBuild(IEnumerable<HeatingInterval> intervals, out HeatingProgram? program)
    {
        if (intervals.Count() > 1)
        {
            for (int i = 0; i < intervals.Count(); i++)
            {
                var first = intervals.ElementAt(i);
                for (int j = i + 1; j < intervals.Count(); j++)
                {
                    var second = intervals.ElementAt(j);
                    if ((second.HoursFrom < first.HoursTo) || (second.HoursFrom == first.HoursTo && second.MinutesFrom < first.MinutesTo))
                    {
                        program = null;
                        return false;
                    }
                }
            }
        }

        program = new HeatingProgram(intervals);
        return true;
    }
}
