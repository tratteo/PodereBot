using System.Reflection.Metadata.Ecma335;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PodereBot.Lib.Commands;

internal abstract class AbstractOpenGateCommand(
    Skin skin,
    GateDriver gateDriver,
    Database db,
    IConfiguration configuration,
    ILogger<AbstractOpenGateCommand> logger,
    GateId gateId
) : Command(skin, configuration)
{
    private readonly GateDriver gateDriver = gateDriver;
    private readonly Database db = db;
    private readonly ILogger<AbstractOpenGateCommand> logger = logger;
    private readonly GateId gateId = gateId;
    private Message? confirmationMessage;

    protected abstract string GateName { get; }
    protected abstract Asset? Asset { get; }

    protected override async Task ExecuteInternal(CommandArguments arguments)
    {
        var admins = configuration.GetSection("Admins").Get<long[]>()?.ToList() ?? [];
        if (admins.Contains(arguments.Message.From!.Id))
        {
            await OpenProcedure(arguments);
        }
        else
        {
            var gatesOpen =
                db.Data.GatesOpenAccessExpirationDate != null
                || db.Data.GatesOpenAccessExpirationDate > DateTime.Now;
            if (!gatesOpen)
            {
                await arguments.Client.SendAsset(arguments.Message, skin.Schema.Unavailable);
                await arguments.Client.SendMessage(
                    arguments.Message.Chat.Id,
                    "I cancelli sono bloccati al momento ❌",
                    disableNotification: true
                );
                return;
            }
            else
            {
                await OpenProcedure(arguments);
            }
        }
    }

    private async Task OpenProcedure(CommandArguments arguments)
    {
        arguments.Client.OnUpdate += (upd) => OnUpdate(arguments, upd);
        confirmationMessage = await arguments.Client.SendMessage(
            arguments.Message.Chat.Id,
            $"Confermi di voler aprire il cancello {GateName}?",
            replyMarkup: new InlineKeyboardMarkup()
                .AddButton("✅", EncodeCallbackQueryData("y"))
                .AddButton("❌", EncodeCallbackQueryData("n")),
            disableNotification: true
        );
    }

    private async Task OnUpdate(CommandArguments arguments, Update update)
    {
        if (!DecodeCallbackQueryData(update.CallbackQuery?.Data, out var data))
            return;

        await arguments.Client.AnswerCallbackQuery(update.CallbackQuery!.Id);

        if (data == "y")
        {
            await arguments.Client.SendChatAction(arguments.Message.Chat.Id, ChatAction.Typing);
            await gateDriver.Open(gateId);
            await arguments.Client.SendAsset(arguments.Message, Asset);
            await arguments.Client.SendMessage(
                arguments.Message.Chat.Id,
                $"Ho aperto il cancello {GateName} 🐱",
                disableNotification: true
            );
        }
        if (confirmationMessage != null)
        {
            await arguments.Client.DeleteMessage(
                confirmationMessage.Chat.Id,
                confirmationMessage.MessageId
            );
        }
    }
}
