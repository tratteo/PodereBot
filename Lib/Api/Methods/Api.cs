using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Humanizer;
using Microsoft.AspNetCore.Http.HttpResults;

namespace PodereBot.Lib.Api;

internal record GetStatusResponse
{
    public TimeSpan RunningTime { get; init; }
    public long MemoryKB { get; init; }
}

internal record PostTemperatureBody
{
    [Required]
    public float? Temperature { get; init; }

    [Required]
    public long? Timestamp { get; init; }

    [Required]
    public string? SensorId { get; init; }
    public string? BoardId { get; init; }

    [Required]
    public string? Location { get; init; }
    public string? Com { get; init; }
}

internal class Api : IEndpoint
{
    public static DateTime FromUnixTimestamp(double timestamp)
    {
        DateTime origin = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
        return origin.AddSeconds(timestamp);
    }

    public static async Task<Results<Ok, BadRequest>> PostTemperature(
        PostTemperatureBody body,
        ILogger<Program> logger,
        ITemperatureDriver temperatureDriver
    )
    {
        temperatureDriver.PostTemperatureReading(
            new TemperatureReading()
            {
                Id = body.SensorId!,
                Temperature = (float)body.Temperature!,
                Timestamp = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds((int)body.Timestamp!),
                Location = body.Location!
            }
        );

        await Task.CompletedTask;
        return TypedResults.Ok();
    }

    public static async Task<Results<Ok<GetStatusResponse>, BadRequest>> GetStatus(ILogger<Program> logger)
    {
        await Task.CompletedTask;
        Process currentProc = Process.GetCurrentProcess();
        var runningTime = DateTime.Now - currentProc.StartTime;
        var memory = currentProc.PrivateMemorySize64 / 1024;
        return TypedResults.Ok(new GetStatusResponse() { MemoryKB = memory, RunningTime = runningTime });
    }

    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("api");
        group
            .MapPost("/temperature", PostTemperature)
            .WithName("temperature")
            .WithDescription("Post a temperature sensor reading")
            .Validate<PostTemperatureBody>();

        group.MapGet("/status", GetStatus).WithName("status").WithDescription("Ping the status of the application");
    }
}
