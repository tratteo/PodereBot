using System.Text.Json;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace PodereBot.Services;

public class DatabaseSchema
{
    [JsonProperty]
    public DateTime? GatesOpenAccessExpirationDate { get; set; } = null;

    public DatabaseSchema Clone() =>
        new() { GatesOpenAccessExpirationDate = GatesOpenAccessExpirationDate };
}

internal class DatabaseService
{
    private static readonly string DB_PATH = Path.Join(AppContext.BaseDirectory, "db.json");
    private readonly ILogger<DatabaseService> logger;

    private readonly DatabaseSchema data;
    public DatabaseSchema Data => data.Clone();

    public DatabaseService(ILogger<DatabaseService> logger)
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
