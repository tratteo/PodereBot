namespace PodereBot.Lib.Trading.Indicators;

public class Stoch(int fastkPeriod = 14, int slowkPeriod = 3, int slowdPeriod = 3) : Indicator
{
    private readonly int fastkPeriod = fastkPeriod;
    private readonly Queue<float> lowQ = new();
    private readonly Queue<float> highQ = new();
    private readonly Ma ma = new(slowkPeriod);
    private readonly Ma slowDMa = new(slowdPeriod);

    public (float?, float?) Last { get; private set; } = (null, null);

    public override void Reset()
    {
        Last = (null, null);
        lowQ.Clear();
        highQ.Clear();
        ma.Reset();
        slowDMa.Reset();
    }

    public (float?, float?) ComputeNext(float close, float low, float high)
    {
        if (highQ.Count < fastkPeriod)
        {
            highQ.Enqueue(high);
            lowQ.Enqueue(low);
            return (null, null);
        }
        else
        {
            highQ.Enqueue(high);
            lowQ.Enqueue(low);
            highQ.Dequeue();
            lowQ.Dequeue();

            var minLow = lowQ.Min();
            var fastk = 100F * (close - minLow) / (highQ.Max() - minLow);
            var slowK = ma.ComputeNext(fastk);
            (float?, float?) res;
            if (slowK is null)
            {
                res = (null, null);
            }
            else
            {
                var slowD = slowDMa.ComputeNext((float)slowK);
                res = slowD is null ? (null, null) : ((float?, float?))(slowK, slowD);
            }
            Last = res;
            return res;
        }
    }
}