using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace PodereBot.Lib.Commands;

internal class UnlockGatesCommand(
    ILogger<UnlockGatesCommand> logger,
    Database database,
    Skin skin,
    IConfiguration configuration
) : Command(skin, configuration)
{
    private readonly ILogger<UnlockGatesCommand> logger = logger;
    private readonly Database database = database;

    protected override async Task ExecuteInternal(CommandArguments arguments)
    {
        arguments.Client.OnUpdate += (upd) => OnUpdate(arguments, upd);
        var kbd = new InlineKeyboardMarkup();
        for (var i = 1; i <= 24; i++)
        {
            kbd.AddButton(i.ToString(), EncodeCallbackQueryData(new { hours = i }));
            if (i % 6 == 0)
            {
                kbd.AddNewRow();
            }
        }
        kbd.AddNewRow();
        kbd.AddButton("Blocca", EncodeCallbackQueryData(new { hours = -1 }));
        await arguments.Client.SendMessage(
            arguments.Message.Chat.Id,
            "Per quante ore vuoi che i cancelli siano abilitati a tutti 🕓?\nSeleziona <b>Blocca</b> per disabilitare l'accesso ai cancelli",
            replyMarkup: kbd,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html,
            disableNotification: true
        );
    }

    private async Task OnUpdate(CommandArguments arguments, Update update)
    {
        if (!DecodeCallbackQueryData(update.CallbackQuery?.Data, out var data))
            return;
        var date = DateTime.Now;
        int hours = data!.hours;
        if (hours < 0)
        {
            database.Edit((data) => data.GatesOpenAccessExpirationDate = null);
            await arguments.Client.SendMessage(
                arguments.Message.Chat.Id,
                $"I cancelli sono bloccati 😼, solo gli amminstratori possono utilizzarli",
                disableNotification: true
            );
        }
        else
        {
            date = date.AddHours(hours);
            database.Edit((data) => data.GatesOpenAccessExpirationDate = date);
            await arguments.Client.SendMessage(
                arguments.Message.Chat.Id,
                $"I cancelli sono aperti fino al {date} 🙀",
                disableNotification: true
            );
        }
        await arguments.Client.AnswerCallbackQuery(update.CallbackQuery!.Id);
    }
}
