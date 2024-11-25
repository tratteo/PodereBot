using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PodereBot.Services;

internal class HeatingDriver(
    ILogger<HeatingDriver> logger,
    IPinDriver pinDriver,
    IConfiguration configuration
)
{
    private readonly ILogger logger = logger;
    private readonly IPinDriver pinDriver = pinDriver;
    private readonly int heatingPin = configuration.GetValue<int>("Pins:Heating");

    public async Task ToggleHeating(bool enabled)
    {
        logger.LogTrace("heating status ping: {a}", enabled);
        if (enabled)
        {
            await pinDriver.PinHigh(heatingPin);
        }
        else
        {
            await pinDriver.PinLow(heatingPin);
        }
    }
}
