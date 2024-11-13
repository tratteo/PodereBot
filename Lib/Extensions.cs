using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using PodereBot.Lib.Commands;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PodereBot.Lib;

public static class Extensions
{
    internal static async Task NotifyOwners(this TelegramBotClient client, string message)
    {
        List<int> ids = [962154266];
        await Task.WhenAll(
            ids.ConvertAll(i => client.SendMessage(i, message, parseMode: ParseMode.Html))
        );
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

    internal static async Task SendAsset(
        this TelegramBotClient client,
        Message message,
        Asset? asset
    )
    {
        if (asset == null)
            return;

        switch (asset.Type)
        {
            case AssetType.video:
                await client.SendVideo(
                    message.Chat.Id,
                    InputFile.FromString(asset.Source),
                    disableNotification: true
                );
                break;
            case AssetType.image:
                break;
            case AssetType.gif:
                await client.SendAnimation(
                    message.Chat.Id,
                    InputFile.FromString(asset.Source),
                    disableNotification: true
                );
                break;
            case AssetType.sticker:
                await client.SendSticker(
                    message.Chat.Id,
                    InputFile.FromString(asset.Source),
                    disableNotification: true
                );
                break;
        }
    }
}
