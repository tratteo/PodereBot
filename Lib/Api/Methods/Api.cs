using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http.HttpResults;

namespace PodereBot.Lib.Api;

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
        DateTime origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
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

    public void MapEndpoint(WebApplication app)
    {
        var group = app.MapGroup("api")
            .MapPost("/temperature", PostTemperature)
            .WithName("temperature")
            .WithDescription("Post a temperature sensor reading")
            .Validate<PostTemperatureBody>();
    }
}
