using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PodereBot.Services;

internal class HeartbeatBackgroundService(
    ILogger<HeartbeatBackgroundService> logger,
    Database db,
    HeatingDriver heatingDriver
) : BackgroundService
{
    private readonly ILogger<HeartbeatBackgroundService> logger = logger;
    private readonly Database db = db;
    private readonly HeatingDriver heatingDriver = heatingDriver;
    private readonly TimeSpan period = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(period);
        while (
            !stoppingToken.IsCancellationRequested
            && await timer.WaitForNextTickAsync(stoppingToken)
        )
        {
            try
            {
                var program = db.Data.HeatingProgram;
                if (program == null)
                {
                    continue;
                }

                await heatingDriver.ToggleHeating(program.IsActive());
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    "Failed to execute HeartbeatBackgroundService with exception message {e}. Good luck next round!",
                    ex.Message
                );
            }
        }
    }
}
