using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace PodereBot.Lib.Commands;

[CommandMetadata(
    Key = "/gates",
    Description = "Abilito o disabilito l'apertura dei cancelli agli utenti 🔐",
    Admin = true
)]
internal class UnlockGatesCommand(
    ILogger<UnlockGatesCommand> logger,
    Database database,
    Skin skin,
    IConfiguration configuration
) : Command(skin, logger, configuration)
{
    private readonly Database database = database;

    protected override async Task ExecuteInternal()
    {
        AttachEvents();
        var kbd = new InlineKeyboardMarkup();
        for (var i = 1; i <= 24; i++)
        {
            kbd.AddButton(i.ToString(), EncodeCallbackQueryData(i));
            if (i % 6 == 0)
            {
                kbd.AddNewRow();
            }
        }
        kbd.AddNewRow();
        kbd.AddButton("Blocca", EncodeCallbackQueryData(-1));
        await Arguments
            .Client.SendMessage(
                Arguments.Message.Chat.Id,
                "Per quante ore vuoi che i cancelli siano abilitati a tutti 🕓?\nSeleziona <b>Blocca</b> per disabilitare l'accesso ai cancelli",
                replyMarkup: kbd,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
                disableNotification: true
            )
            .DeleteOnDetach(this);
    }

    protected override async Task OnCallback(Update update, string callbackData)
    {
        var date = DateTime.Now;
        int hours = int.Parse(callbackData);
        if (hours < 0)
        {
            database.Edit((data) => data.GatesOpenAccessExpirationDate = null);
            await Arguments.Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"I cancelli sono bloccati 😼, solo gli amminstratori possono utilizzarli",
                disableNotification: true
            );
        }
        else
        {
            date = date.AddHours(hours);
            database.Edit((data) => data.GatesOpenAccessExpirationDate = date);
            await Arguments.Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"I cancelli sono aperti fino al {date} 🙀",
                disableNotification: true
            );
        }
        await Arguments.Client.AnswerCallbackQuery(update.CallbackQuery!.Id);
        await DetachEvents();
    }
}
