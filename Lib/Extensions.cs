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

    internal static async Task<Message?> SendAssetAsync(
        this TelegramBotClient client,
        ChatId chatId,
        Asset? asset,
        IReplyMarkup? replyMarkup = null,
        string? caption = null
    )
    {
        if (asset == null)
            return null;

        return asset.Type switch
        {
            AssetType.video
                => await client.SendVideoAsync(
                    chatId,
                    InputFile.FromString(asset.Source),
                    replyMarkup: replyMarkup,
                    caption: caption
                ),
            AssetType.gif
                => await client.SendAnimationAsync(
                    chatId,
                    InputFile.FromString(asset.Source),
                    replyMarkup: replyMarkup,
                    caption: caption
                ),
            AssetType.sticker
                => await client.SendStickerAsync(
                    chatId,
                    InputFile.FromString(asset.Source),
                    replyMarkup: replyMarkup
                ),
            AssetType.image
                => await client.SendPhotoAsync(
                    chatId,
                    InputFile.FromString(asset.Source),
                    replyMarkup: replyMarkup,
                    caption: caption
                ),
            _ => null,
        };
    }
}
