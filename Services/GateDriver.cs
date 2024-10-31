using System.Runtime.CompilerServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PodereBot.Services;

internal class GateDriver(ILogger<GateDriver> logger, Serial serialCom, IConfiguration configuration)
{
    public enum GateId
    {
        automatic,
        pedestrian
    }
    private readonly ILogger logger = logger;
    private readonly Serial serialCom = serialCom;
    private readonly IConfiguration configuration = configuration;

    public async Task Open(GateId gate)
    {
        int? pin = null;
        switch (gate)
        {
            case GateId.automatic:
                pin = configuration.GetValue<int>("AUTOMATIC_GATE_PIN");
                break;
            case GateId.pedestrian:
                pin = configuration.GetValue<int>("PEDESTRIAN_GATE_PIN");
                break;

        }
        serialCom.Write($"h{pin}");
        await Task.Delay(1000);
        serialCom.Write($"l{pin}");

    }
}