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
    SkinService skin,
    GateDriverService gateDriver,
    DatabaseService db,
    IConfiguration configuration,
    ILogger<AbstractOpenGateCommand> logger,
    GateDriverService.GateId gateId
) : Command(skin, configuration)
{
    private readonly GateDriverService gateDriver = gateDriver;
    private readonly DatabaseService db = db;
    private readonly ILogger<AbstractOpenGateCommand> logger = logger;
    private readonly GateDriverService.GateId gateId = gateId;
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
                await arguments.Client.SendAssetAsync(arguments.Message, skin.Schema.Unavailable);
                await arguments.Client.SendTextMessageAsync(
                    arguments.Message.Chat.Id,
                    "I cancelli sono bloccati al momento ❌"
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
        confirmationMessage = await arguments.Client.SendTextMessageAsync(
            arguments.Message.Chat.Id,
            $"Confermi di voler aprire il cancello {GateName}?",
            replyMarkup: new InlineKeyboardMarkup()
                .AddButton("✅", EncodeCallbackQueryData("y"))
                .AddButton("❌", EncodeCallbackQueryData("n"))
        );
    }

    private async Task OnUpdate(CommandArguments arguments, Update update)
    {
        if (!DecodeCallbackQueryData(update.CallbackQuery?.Data, out var data))
            return;

        await arguments.Client.AnswerCallbackQueryAsync(update.CallbackQuery!.Id);

        if (data == "y")
        {
            await arguments.Client.SendChatActionAsync(
                arguments.Message.Chat.Id,
                ChatAction.Typing
            );
            await gateDriver.Open(gateId);
            await arguments.Client.SendAssetAsync(arguments.Message, Asset);
            await arguments.Client.SendTextMessageAsync(
                arguments.Message.Chat.Id,
                $"Ho aperto il cancello {GateName} 🐱"
            );
        }
        if (confirmationMessage != null)
        {
            await arguments.Client.DeleteMessageAsync(
                confirmationMessage.Chat.Id,
                confirmationMessage.MessageId
            );
        }
    }
}
