﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text.Json;

namespace PodereBot.Services;

public class DatabaseSchema
{
    [JsonProperty]
    public DateTime? GatesOpenAccessExpirationDate { get; set; } = null;

    public DatabaseSchema Clone() => new DatabaseSchema { GatesOpenAccessExpirationDate = GatesOpenAccessExpirationDate };
}
internal class Database
{
    static private readonly string DB_PATH = Path.Join(AppContext.BaseDirectory, "db.json");
    private readonly ILogger<Database> logger;

    private readonly DatabaseSchema data;
    public DatabaseSchema Data => data.Clone();
    public Database(ILogger<Database> logger)
    {
        this.logger = logger;
        if (File.Exists(DB_PATH))
        {
            data = JsonConvert.DeserializeObject<DatabaseSchema>(File.ReadAllText(DB_PATH))!;
            logger.LogDebug("local db found and loaded");
        }
        else
        {
            data = new DatabaseSchema();
            logger.LogDebug("local db not found");
        }
    }

    private void Save()
    {
        File.WriteAllTextAsync(DB_PATH, JsonConvert.SerializeObject(data));
        logger.LogDebug("local db serialized > {p}", DB_PATH);
    }



    public void Edit(Action<DatabaseSchema> modifier)
    {
        modifier?.Invoke(data);
        Save();
    }

}
