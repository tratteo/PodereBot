﻿using Newtonsoft.Json;

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
    [JsonProperty(Required = Required.Always)]
    public required string Source { get; init; }

    [JsonProperty(Required = Required.Always)]
    public required AssetType Type { get; init; }
}

internal class Metadata
{
    public required string Name { get; init; }
    public required string? Author { get; init; }
}

internal class SkinSchema
{
    [JsonProperty(Required = Required.Always)]
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
            Start = new Asset() { Source = "https://media1.tenor.com/m/NXMs9_FlGpcAAAAd/rage-emoji-rage.gif", Type = AssetType.gif },
            Unauthorized = new Asset() { Source = "https://media1.tenor.com/m/SMiE27y-ExsAAAAd/ban-banned.gif", Type = AssetType.gif },
            AutomaticGateOpen = null,
            PedestrianGateOpen = null,
            Unavailable = null,
            GatesLight = null,
        };
    }
}

internal class Skin
{
    private readonly ILogger<Skin> logger;
    private readonly Database db;

    public SkinSchema Schema { get; private set; }

    public Skin(ILogger<Skin> logger, Database db)
    {
        this.logger = logger;
        this.db = db;
        var skinName = db.Data.ActiveSkin;
        var skinPath = Path.Join(AppContext.BaseDirectory, Globals.SKINS_PATH, skinName);
        if (skinName != null && File.Exists(skinPath))
        {
            Schema = JsonConvert.DeserializeObject<SkinSchema>(File.ReadAllText(skinPath))!;
            logger.LogDebug("active skin {s} loaded", skinName);
        }
        else
        {
            Schema = SkinSchema.Default();
            logger.LogDebug("using default skin");
        }
    }

    public void SetSkin(string skinPath, SkinSchema skinSchema)
    {
        db.Edit(data =>
        {
            data.ActiveSkin = skinPath;
        });
        Schema = skinSchema;
    }

    public List<(string path, SkinSchema schema)> GetRegisteredSkins()
    {
        List<(string, SkinSchema)> schemas = [];
        foreach (var skinFile in Directory.GetFiles(Path.Join(AppContext.BaseDirectory, Globals.SKINS_PATH)))
        {
            try
            {
                var schema = JsonConvert.DeserializeObject<SkinSchema>(File.ReadAllText(skinFile));
                if (schema != null)
                {
                    schemas.Add((Path.GetFileName(skinFile)!, schema));
                }
            }
            catch (Exception)
            {
                logger.LogWarning("deleting skin at location {s} due to bad format", skinFile);
                File.Delete(skinFile);
                continue;
            }
        }
        return schemas;
    }
}
