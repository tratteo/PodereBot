namespace PodereBot.Lib.Trading.Indicators;

public class Macd(int fast = 12, int slow = 26, int signal = 9) : Indicator
{
    private readonly Ema emaFast = new(fast);
    private readonly Ema emaSlow = new(slow);
    private readonly Ema emaSignal = new(signal);

    public (float?, float?, float?) Last { get; private set; } = (null, null, null);

    public override void Reset()
    {
        Last = (null, null, null);
        emaFast.Reset();
        emaSlow.Reset();
        emaSignal.Reset();
    }

    /// <summary>
    /// </summary>
    /// <param name="close"> </param>
    /// <returns> (hist, fast, slow) </returns>
    public (float?, float?, float?) ComputeNext(float close)
    {
        var emaF = emaFast.ComputeNext(close);
        var emaS = emaSlow.ComputeNext(close);

        if (emaF is null || emaS is null)
        {
            return (null, null, null);
        }
        var macd = emaF - emaS;

        var sig = emaSignal.ComputeNext((float)macd);
        var res = (sig is null ? null : macd - sig, macd, sig);
        Last = res;
        return res;
    }
}