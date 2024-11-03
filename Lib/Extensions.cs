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
            "🤬",
            "😢",
            "🎉",
            "🤩",
            "🤮",
            "💩",
            "🙏",
            "👌",
            "🕊",
            "🤡",
            "🥱",
            "🥴",
            "😍",
            "🐳",
            "❤‍🔥",
            "🌚",
            "🌭",
            "💯",
            "🤣",
            "⚡",
            "🍌",
            "🏆",
            "💔",
            "🤨",
            "😐",
            "🍓",
            "🍾",
            "💋",
            "🖕",
            "😈",
            "😴",
            "😭",
            "🤓",
            "👻",
            "👨‍💻",
            "👀",
            "🎃",
            "🙈",
            "😇",
            "😨",
            "🤝",
            "✍",
            "🤗",
            "🫡",
            "🎅",
            "🎄",
            "☃",
            "💅",
            "🤪",
            "🗿",
            "🆒",
            "💘",
            "🙉",
            "🦄",
            "😘",
            "💊",
            "🙊",
            "😎",
            "👾",
            "🤷‍♂",
            "🤷",
            "🤷‍♀",
            "😡"
        ];
        Random random = new();
        return new ReactionTypeEmoji() { Emoji = available[random.Next(0, available.Count)] };
    }

    internal static async Task SendAssetAsync(
        this TelegramBotClient client,
        Message message,
        Asset? asset,
        IReplyMarkup? replyMarkup = null,
        string? caption = null
    )
    {
        if (asset == null)
            return;

        switch (asset.Type)
        {
            case AssetType.video:
                await client.SendVideoAsync(
                    message.Chat.Id,
                    InputFile.FromString(asset.Source),
                    replyMarkup: replyMarkup,
                    caption: caption
                );
                break;
            case AssetType.image:
                break;
            case AssetType.gif:
                await client.SendAnimationAsync(
                    message.Chat.Id,
                    InputFile.FromString(asset.Source),
                    replyMarkup: replyMarkup,
                    caption: caption
                );
                break;
            case AssetType.sticker:
                await client.SendStickerAsync(
                    message.Chat.Id,
                    InputFile.FromString(asset.Source),
                    replyMarkup: replyMarkup
                );
                break;
        }
    }
}
