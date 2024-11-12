using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PodereBot.Lib.Commands;

[CommandMetadata(Key = "/setskin", Description = "Cambia la mia skin 🎨")]
internal class SetSkinCommand(
    ILogger<UploadSkinCommand> logger,
    Skin skin,
    IConfiguration configuration
) : Command(skin, logger, configuration)
{
    private readonly ILogger<UploadSkinCommand> logger = logger;
    private readonly List<(string path, SkinSchema schema)> skins = skin.GetRegisteredSkins();

    protected override async Task ExecuteInternal()
    {
        AttachEvents();
        var kbd = new InlineKeyboardMarkup();
        for (int i = 0; i < skins.Count; i++)
        {
            var (path, schema) = skins[i];
            kbd.AddButton(
                $"{schema.Metadata.Name} ({schema.Metadata.Author})",
                EncodeCallbackQueryData(i)
            );
            if ((i + 1) % 3 == 0)
            {
                kbd.AddNewRow();
            }
        }
        kbd.AddNewRow();
        kbd.AddButton("Annulla", EncodeCallbackQueryData("cancel"));
        var skinData = $"{skin.Schema.Metadata.Name} ({skin.Schema.Metadata.Author})";
        await Arguments
            .Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"""
            Skin attualmente in uso: <b>{skinData}</b>
            Quale skin vuoi impostare 🎨? 
            """,
                parseMode: ParseMode.Html,
                replyMarkup: kbd,
                disableNotification: true
            )
            .DeleteOnDetach(this);
    }

    protected override async Task OnCallback(Update update, string callbackData)
    {
        if (callbackData != "cancel")
        {
            var (path, schema) = skins.ElementAt(int.Parse(callbackData));
            skin.SetSkin(path, schema);
            await Arguments.Client.AnswerCallbackQuery(update.CallbackQuery!.Id);
            await Arguments.Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"🟢 Sto usando la skin <b>{schema.Metadata.Name} di {schema.Metadata.Author}</b>",
                parseMode: ParseMode.Html
            );
        }
        await DetachEvents();
    }
}
