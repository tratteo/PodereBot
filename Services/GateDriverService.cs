using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PodereBot.Services;

internal class GateDriverService(
    ILogger<GateDriverService> logger,
    SerialService serialCom,
    IConfiguration configuration
)
{
    public enum GateId
    {
        automatic,
        pedestrian
    }

    private readonly ILogger logger = logger;
    private readonly SerialService serialCom = serialCom;
    private readonly IConfiguration configuration = configuration;

    public async Task ToggleLights()
    {
        int? pin = configuration.GetValue<int>("Serial:GatesLightPin");
        logger.LogDebug("serial pin: {p}", pin);
        serialCom.Write($"h{pin}");
        await Task.Delay(1000);
        serialCom.Write($"l{pin}");
    }

    public async Task Open(GateId gate)
    {
        int? pin = null;
        switch (gate)
        {
            case GateId.automatic:
                pin = configuration.GetValue<int>("Serial:AutomaticGatePin");
                break;
            case GateId.pedestrian:
                pin = configuration.GetValue<int>("Serial:PedestrianGatePin");
                break;
        }
        logger.LogDebug("serial pin: {p}", pin);
        serialCom.Write($"h{pin}");
        await Task.Delay(1000);
        serialCom.Write($"l{pin}");
    }
}
