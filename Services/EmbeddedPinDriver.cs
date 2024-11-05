using System.Device.Gpio;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PodereBot.Services;

internal class EmbeddedPinDriver : IPinDriver
{
    private readonly GpioController gpioController;
    private readonly ILogger<EmbeddedPinDriver> logger;
    private readonly IConfiguration configuration;

    public EmbeddedPinDriver(ILogger<EmbeddedPinDriver> logger, IConfiguration configuration)
    {
        gpioController = new GpioController();
        for (int i = 0; i < gpioController.PinCount; i++)
        {
            gpioController.OpenPin(i);
            // if (gpioController.IsPinModeSupported(i, PinMode.Output))
            // {
            //     gpioController.SetPinMode(i, PinMode.Output);
            // }
        }
        this.logger = logger;
        this.configuration = configuration;
    }

    public void Dispose()
    {
        for (int i = 0; i < gpioController.PinCount; i++)
        {
            gpioController.ClosePin(i);
        }
        gpioController.Dispose();
    }

    public Task PinHigh(int? pin)
    {
        if (pin == null)
            return Task.CompletedTask;

        try
        {
            gpioController.Write((int)pin, PinValue.High);
            logger.LogTrace("embedded pin {p}: high", pin);
        }
        catch (Exception ex)
        {
            logger.LogTrace(ex, "error accessing pin {p}", pin);
        }
        return Task.CompletedTask;
    }

    public Task PinLow(int? pin)
    {
        if (pin == null)
            return Task.CompletedTask;

        try
        {
            gpioController.Write((int)pin, PinValue.Low);
            logger.LogTrace("embedded pin {p}: low", pin);
        }
        catch (Exception ex)
        {
            logger.LogTrace(ex, "error accessing pin {p}", pin);
        }
        return Task.CompletedTask;
    }
}
