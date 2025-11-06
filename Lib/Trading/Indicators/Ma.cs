namespace PodereBot.Lib.Trading.Indicators;

public class Ma(int period = 20) : Indicator
{
    private readonly int period = period;
    private readonly Queue<float> firstValues = new();

    public float? Last { get; private set; } = null;

    public float? ComputeNext(float close)
    {
        firstValues.Enqueue(close);
        if (firstValues.Count > period)
        {
            firstValues.Dequeue();
        }

        float? res = firstValues.Count < period ? null : firstValues.Average();
        Last = res;
        return res;
    }

    public override void Reset()
    {
        Last = null;
        firstValues.Clear();
    }
}