using Microsoft.AspNetCore.Mvc.Diagnostics;

namespace PodereBot.Lib.Common;

public class HeatingProgram()
{
    public List<DayInterval> Intervals { get; private set; } = [];

    private HeatingProgram(IEnumerable<DayInterval> intervals)
        : this()
    {
        Intervals = intervals.ToList();
    }

    public override string ToString()
    {
        return string.Join("\n", Intervals);
    }

    public bool IsActive()
    {
        var date = DateTime.Now;
        var dayTimestamp = date.Hour * 3600 + date.Minute * 60;
        return Intervals.Any(i => i.FromTimestamp <= dayTimestamp && i.ToTimestamp > dayTimestamp);
    }

    public static bool TryBuild(IEnumerable<DayInterval> intervals, out HeatingProgram? program)
    {
        if (intervals.Count() > 1)
        {
            for (int i = 0; i < intervals.Count(); i++)
            {
                var first = intervals.ElementAt(i);
                for (int j = i + 1; j < intervals.Count(); j++)
                {
                    var second = intervals.ElementAt(j);
                    if (
                        (second.HoursFrom < first.HoursTo)
                        || (
                            second.HoursFrom == first.HoursTo
                            && second.MinutesFrom < first.MinutesTo
                        )
                    )
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
