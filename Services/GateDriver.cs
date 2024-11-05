using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PodereBot.Services;

internal class GateDriver(
    ILogger<GateDriver> logger,
    IPinDriver pinDriver,
    IConfiguration configuration
)
{
    public enum GateId
    {
        automatic,
        pedestrian
    }

    private readonly ILogger logger = logger;
    private readonly IPinDriver pinDriver = pinDriver;
    private readonly IConfiguration configuration = configuration;

    public async Task ToggleLights()
    {
        int? pin = configuration.GetValue<int>("Pins:GatesLight");
        logger.LogDebug("serial pin: {p}", pin);
        await pinDriver.PinHigh(pin);
        await Task.Delay(1000);
        await pinDriver.PinLow(pin);
    }

    public async Task Open(GateId gate)
    {
        int? pin = null;
        switch (gate)
        {
            case GateId.automatic:
                pin = configuration.GetValue<int>("Pins:AutomaticGate");
                break;
            case GateId.pedestrian:
                pin = configuration.GetValue<int>("Pins:PedestrianGate");
                break;
        }
        await pinDriver.PinHigh(pin);
        await Task.Delay(1000);
        await pinDriver.PinLow(pin);
    }
}
