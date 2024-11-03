using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace PodereBot.Services;

internal enum AssetType
{
    video,
    gif,
    image,
    sticker
}

internal class Asset
{
    public required string Source { get; init; }
    public required AssetType Type { get; init; }
}

internal class Metadata
{
    public required string Name { get; init; }
    public required string? Author { get; init; }
}

internal class SkinSchema
{
    public required Metadata Metadata { get; init; }
    public required Asset? Start { get; init; }
    public required Asset? Unauthorized { get; init; }
    public required Asset? AutomaticGateOpen { get; init; }
    public required Asset? PedestrianGateOpen { get; init; }
    public required Asset? GatesLight { get; init; }
    public required Asset? Unavailable { get; init; }

    public static SkinSchema Default()
    {
        return new SkinSchema()
        {
            Metadata = new Metadata() { Name = "Default", Author = "trat" },
            Start = new Asset()
            {
                Source = "https://media1.tenor.com/m/NXMs9_FlGpcAAAAd/rage-emoji-rage.gif",
                Type = AssetType.gif
            },
            Unauthorized = new Asset()
            {
                Source = "https://media1.tenor.com/m/SMiE27y-ExsAAAAd/ban-banned.gif",
                Type = AssetType.gif
            },
            AutomaticGateOpen = null,
            PedestrianGateOpen = null,
            Unavailable = null,
            GatesLight = null,
        };
    }
}

internal class SkinService
{
    private readonly ILogger<SkinService> logger;
    public SkinSchema Schema { get; init; }

    public SkinService(ILogger<SkinService> logger, IConfiguration configuration)
    {
        this.logger = logger;
        var skinName = configuration.GetValue<string>("Skin");
        var skinPath = Path.Join(AppContext.BaseDirectory, "Skins", skinName);
        if (System.IO.File.Exists(skinPath))
        {
            Schema = JsonConvert.DeserializeObject<SkinSchema>(
                System.IO.File.ReadAllText(skinPath)
            )!;
            logger.LogDebug("active skin {s} loaded", skinName);
        }
        else
        {
            Schema = SkinSchema.Default();
            logger.LogDebug("using default skin");
        }
    }
}
