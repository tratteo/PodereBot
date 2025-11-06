namespace PodereBot.Lib.Trading.Indicators;

public class Ema(int period = 20, int smoothing = 2) : Indicator
{
    private readonly int period = period;
    private readonly int smoothing = smoothing;
    private readonly Queue<float> firstValues = new();
    private float previousValue = 0;

    public float? Last { get; private set; } = null;

    public float? ComputeNext(float close)
    {
        if (firstValues.Count < period)
        {
            firstValues.Enqueue(close);
            previousValue = firstValues.Average();
            return null;
        }
        else
        {
            var smooth = smoothing / (1F + period);
            previousValue = close * smooth + previousValue * (1F - smooth);
            Last = previousValue;
            return previousValue;
        }
    }

    public override void Reset()
    {
        Last = null;
        previousValue = 0;
        firstValues.Clear();
    }
}