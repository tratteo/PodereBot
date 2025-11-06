namespace PodereBot.Lib.Trading.Indicators;

public class StochRsi(int period = 14, int fastkPeriod = 3, int slowdPeriod = 3) : Indicator
{
    private readonly Rsi rsi = new(period);
    private readonly Stoch stoch = new(period, 3, fastkPeriod);
    private readonly Ma dMa = new(slowdPeriod);

    public (float?, float?) Last { get; private set; } = (null, null);

    public override void Reset()
    {
        Last = (null, null);
        rsi.Reset();
        stoch.Reset();
        dMa.Reset();
    }

    public (float?, float?) ComputeNext(float close)
    {
        float? fastD;
        var current = rsi.ComputeNext(close);
        fastD = current is null ? null : stoch.ComputeNext((float)current, (float)current, (float)current).Item1;
        if (fastD is not null)
        {
            var slowD = dMa.ComputeNext((float)fastD);
            var res = slowD is not null ? ((float?, float?))(fastD, slowD) : (null, null);
            Last = res;
            return res;
        }
        else
        {
            return (null, null);
        }
    }
}