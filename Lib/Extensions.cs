using System.Reflection;
using Binance.Net.Enums;
using CryptoExchange.Net.Attributes;
using Microsoft.OpenApi.Extensions;
using PodereBot.Lib.Api;
using PodereBot.Lib.Commands;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PodereBot.Lib;

public static class Extensions
{
    public static string GetMapName(this KlineInterval interval)
    {
        var attr = interval.GetAttributeOfType<MapAttribute>();
        if (attr.Values.Length > 0)
        {
            return attr.Values[0];
        }
        return interval.GetDisplayName();
    }

    public static bool IsLocal(this HttpRequest req)
    {
        var connection = req.HttpContext.Connection;
        if (connection.RemoteIpAddress == null)
            return false;

        var bytes = connection.RemoteIpAddress.GetAddressBytes();
        if (bytes.Length < 4)
            return false;

        return (bytes[0] == 192 && bytes[1] == 168) || (bytes[0] == 127 && bytes[1] == 0 && bytes[2] == 0 && bytes[3] == 1);
    }

    internal static void UseApiEndpoints(this WebApplication app)
    {
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
        {
            if (!typeof(IEndpoint).IsAssignableFrom(type) || type.IsInterface || type.IsAbstract)
                continue;
            var endpoint = Activator.CreateInstance(type) as IEndpoint;
            endpoint?.MapEndpoint(app);
        }
    }

    internal static async Task NotifyUsers(this TelegramBotClient client, string message, List<long> ids, ILogger? logger = null)
    {
        try
        {
            await Task.WhenAll(ids.ConvertAll(i => client.SendMessage(i, message, parseMode: ParseMode.Html)));
        }
        catch (Exception ex)
        {
            logger?.LogError("error notifying users {e}", ex);
        }
    }
    internal static async Task NotifyOwners(this TelegramBotClient client, string message, ILogger? logger = null)
    {
        try
        {
            List<int> ids = [962154266];
            await Task.WhenAll(ids.ConvertAll(i => client.SendMessage(i, message, parseMode: ParseMode.Html)));
        }
        catch (Exception ex)
        {
            logger?.LogError("error notifying admins {e}", ex);
        }
    }

    internal static async Task<Message> CleanupOnDetach(this Task<Message> msgTask, Command command)
    {
        var msg = await msgTask;
        command.AddToCleanupMd(msg);
        return msg;
    }

    internal static async Task<Message> DeleteOnDetach(this Task<Message> msgTask, Command command)
    {
        var msg = await msgTask;
        command.AddToRemove(msg);
        return msg;
    }

    internal static object ReflectSchema(this Type type)
    {
        var properties = type.GetProperties();

        var schema = new Dictionary<string, object>();
        foreach (var property in properties)
        {
            var propertyType = property.PropertyType;
            if (propertyType.IsEnum)
            {
                schema.Add(property.Name, string.Join("|", Enum.GetNames(propertyType)));
            }
            else if (propertyType.IsPrimitive || propertyType == typeof(string))
            {
                schema.Add(property.Name, propertyType.FullName ?? "?");
            }
            else
            {
                schema.Add(property.Name, propertyType.ReflectSchema());
            }
        }

        return schema;
    }

    internal static List<CommandRegistryKey> GetCommands(this Assembly assembly)
    {
        var commands = new List<CommandRegistryKey>();
        foreach (var type in assembly.GetTypes())
        {
            var attribute = type.GetCustomAttribute<CommandMetadataAttribute>();
            if (attribute == null || !type.IsSubclassOf(typeof(Command)) || type.IsAbstract)
                continue;
            commands.Add(new CommandRegistryKey(attribute, type));
        }
        return commands;
    }

    internal static void AddCommands(this IServiceCollection services)
    {
        var commands = Assembly.GetExecutingAssembly().GetCommands();
        foreach (var c in commands)
        {
            services.AddTransient(c.commandType);
        }
    }

    internal static ReactionTypeEmoji RandomEmoji(this Message emoji)
    {
        List<string> available =
        [
            "👍",
            "❤",
            "🔥",
            "🥰",
            "👏",
            "😁",
            "🤔",
            "🤯",
            "😱",
            "🎉",
            "🤩",
            "🙏",
            "👌",
            "🤡",
            "🥱",
            "🥴",
            "😍",
            "❤‍🔥",
            "🌭",
            "💯",
            "🤣",
            "⚡",
            "🍌",
            "🏆",
            "🍾",
            "😈",
            "🤓",
            "👻",
            "👀",
            "🎃",
            "🙈",
            "😇",
            "🤝",
            "🤗",
            "🫡",
            "🎅",
            "🤪",
            "🗿",
            "🙉",
            "😘",
            "🙊",
            "😎",
            "👾"
        ];
        Random random = new();
        return new ReactionTypeEmoji() { Emoji = available[random.Next(0, available.Count)] };
    }

    internal static async Task SendAsset(this TelegramBotClient client, Message message, Asset? asset)
    {
        if (asset == null)
            return;

        switch (asset.Type)
        {
            case AssetType.video:
                await client.SendVideo(message.Chat.Id, InputFile.FromString(asset.Source), disableNotification: true);
                break;
            case AssetType.image:
                break;
            case AssetType.gif:
                await client.SendAnimation(message.Chat.Id, InputFile.FromString(asset.Source), disableNotification: true);
                break;
            case AssetType.sticker:
                await client.SendSticker(message.Chat.Id, InputFile.FromString(asset.Source), disableNotification: true);
                break;
        }
    }
}
