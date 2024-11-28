using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PodereBot.Services;

internal class HeatingProgramDaemon(
    ILogger<HeatingProgramDaemon> logger,
    IPinDriver pinDriver,
    ITemperatureReader temperatureReader,
    IConfiguration configuration,
    Database db
) : BackgroundService
{
    private readonly ILogger<HeatingProgramDaemon> logger = logger;
    private readonly Database db = db;
    private readonly IPinDriver pinDriver = pinDriver;
    private readonly ITemperatureReader temperatureReader = temperatureReader;
    private readonly int heatingPin = configuration.GetValue<int>("Pins:Heating");
    private readonly TimeSpan daemonPollInterval = TimeSpan.FromSeconds(configuration.GetValue<int?>("Heating:PollIntervalSeconds") ?? 10);
    private readonly TimeSpan thresholdTolerance = TimeSpan.FromSeconds(configuration.GetValue<int?>("Heating:ToleranceSeconds") ?? 600);
    private TimeSpan actionDelay = TimeSpan.Zero;

    public async Task ToggleHeating(bool enabled)
    {
        if (enabled == db.Data.HeatingActive)
            return;

        actionDelay = TimeSpan.Zero;
        db.Edit(d => d.HeatingActive = enabled);
        logger.LogInformation("setting heating: {h}", enabled);
        if (enabled)
        {
            await pinDriver.PinHigh(heatingPin);
        }
        else
        {
            await pinDriver.PinLow(heatingPin);
        }
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var temperature = await temperatureReader.GetTemperature(cancellationToken);
        var interval = db.Data.HeatingProgram?.GetActiveInterval();
        if (temperature != null && interval != null && temperature < interval.Temperature - 1)
        {
            await ToggleHeating(true);
        }
        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(daemonPollInterval);
        while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                //logger.LogInformation("heating daemon beat");
                var program = db.Data.HeatingProgram;
                if (program == null)
                {
                    continue;
                }
                var temperature = await temperatureReader.GetTemperature(stoppingToken);
                if (temperature == null)
                {
                    logger.LogWarning("no temperature data");
                    continue;
                }
                logger.LogTrace("heating: {h}, temp: {t}", db.Data.HeatingActive, temperature);
                var interval = program.GetActiveInterval();
                if (interval == null)
                {
                    await ToggleHeating(false);
                    continue;
                }

                if (db.Data.HeatingActive)
                {
                    if (temperature > interval!.Temperature && actionDelay >= thresholdTolerance)
                    {
                        await ToggleHeating(false);
                    }
                    else if (temperature <= interval!.Temperature)
                    {
                        actionDelay = TimeSpan.Zero;
                    }
                }
                else
                {
                    if (temperature < interval!.Temperature && actionDelay >= thresholdTolerance)
                    {
                        await ToggleHeating(true);
                    }
                    else if (temperature >= interval!.Temperature)
                    {
                        actionDelay = TimeSpan.Zero;
                    }
                }

                actionDelay = actionDelay.Add(daemonPollInterval);
            }
            catch (Exception ex)
            {
                logger.LogWarning("failed with exception message {e}. Good luck next round!", ex.Message);
            }
        }
    }
}
