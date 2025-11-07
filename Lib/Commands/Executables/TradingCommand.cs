using System.Data.Common;
using System.Text;
using CryptoExchange.Net.SharedApis;
using Newtonsoft.Json;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PodereBot.Lib.Commands;

[CommandMetadata(Key = "/trading", Description = "Gestisci gli alert di trading sulla strategia corrente")]
internal class TradingCommand(Skin skin, ILogger<StartCommand> logger, IConfiguration configuration, Database database, CryptoAlertDaemon alertDaemon) : Command(skin, logger, configuration)
{
    protected override async Task ExecuteInternal()
    {
        logger.LogInformation("kline: {k}", alertDaemon.LastKline);
        var subscribed = Arguments.Message.From?.Id != null && database.Data.TradingAlertsSubscriptions?.Contains(Arguments.Message.From.Id) == true;
        var str = new StringBuilder($"""
            <b>🟢 Strategia attiva</b>

            Strategy: <b>AtrStochRsiEmaStrategy</b>
            Interval: <b>{alertDaemon.Interval.GetMapName()}</b>
            Pair: <b>{alertDaemon.Pair}</b>

            <b>Last Kline registered</b>
            <pre language='json'>{JsonConvert.SerializeObject(alertDaemon.LastKline, Formatting.Indented)}</pre>
            """);
        str.Append(new string('_', 30));
        str.AppendLine();
        var kbd = new InlineKeyboardMarkup();
        if (subscribed)
        {
            str.AppendLine("\n<b>✅ Sei iscritto alla lista degli alerts</b>");
            kbd.AddButton("Disattiva alerts", EncodeCallbackQueryData("unsub"));
        }
        else
        {
            str.AppendLine("\n<b>❌ Non sei iscritto alla lista degli alerts</b>");
            kbd.AddButton("Attiva alerts", EncodeCallbackQueryData("sub"));
        }
        kbd.AddNewRow().AddButton("Chiudi", EncodeCallbackQueryData("close"));
        str.AppendLine("Invio periodicamente agli iscritti i segnali di <b>Buy</b> e <b>Sell</b> quando le condizioni definite dalla strategia sono soddisfatte.");

        AttachEvents();

        await Arguments.Client.SendMessage(
            Arguments.Message.Chat.Id,
            str.ToString(),
            parseMode: ParseMode.Html,
            replyMarkup: kbd,
            disableNotification: true
        ).DeleteOnDetach(this);

    }

    protected override async Task OnCallback(Update update, string callbackData)
    {
        await Arguments.Client.AnswerCallbackQuery(update.CallbackQuery!.Id);
        if (update.CallbackQuery?.From?.Id == null)
        {
            await Arguments.Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"Non ho trovato il tuo id utente, sicuro che tu esista 🤔?",
                disableNotification: true
            );

        }
        else if (callbackData == "sub")
        {
            await Arguments.Client.SendChatAction(Arguments.Message.Chat.Id, ChatAction.Typing);
            database.Edit((db) =>
            {
                db.TradingAlertsSubscriptions ??= [];
                db.TradingAlertsSubscriptions.Add(update.CallbackQuery!.From!.Id);
            });

            await Arguments.Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"Ti ho iscritto alla lista degli alerts 💸",
                disableNotification: true
            );

        }
        else if (callbackData == "unsub")
        {
            await Arguments.Client.SendChatAction(Arguments.Message.Chat.Id, ChatAction.Typing);
            database.Edit((db) =>
            {
                db.TradingAlertsSubscriptions ??= [];
                db.TradingAlertsSubscriptions.RemoveAll((c) => c == update.CallbackQuery!.From!.Id);
            });

            await Arguments.Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"Ti ho rimosso dalla lista degli alerts",
                disableNotification: true
            );

        }

        await DetachEvents();
    }
}
