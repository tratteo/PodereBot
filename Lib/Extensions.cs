using Microsoft.Extensions.DependencyInjection;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace PodereBot.Lib;

public static class Extensions
{
    public static void AddCommands(this IServiceCollection services)
    {
        foreach (var c in Registry.commands)
        {
            services.AddTransient(c.commandType);
        }
    }

    public static ReactionTypeEmoji RandomEmoji(this Message emoji)
    {
        List<string> available =
        [
            "👍",
            "👎",
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
