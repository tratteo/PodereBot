using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PodereBot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace PodereBot.Lib.Commands.Abstract;

internal abstract class AbstractOpenGateCommand(
    Skin skin,
    GateDriver gateDriver,
    Database db,
    IConfiguration configuration,
    ILogger<AbstractOpenGateCommand> logger,
    GateId gateId
) : Command(skin, logger, configuration)
{
    private readonly GateDriver gateDriver = gateDriver;
    private readonly Database db = db;
    private readonly GateId gateId = gateId;
    private readonly List<long> adminIds =
        configuration.GetSection("Admins").Get<long[]>()?.ToList() ?? [];
    protected abstract string GateName { get; }
    protected abstract Asset? Asset { get; }

    protected override async Task ExecuteInternal()
    {
        if (adminIds.Contains(Arguments.Message.From!.Id))
        {
            await OpenProcedure();
        }
        else
        {
            var canOpen =
                db.Data.GatesOpenAccessExpirationDate != null
                && DateTime.Now < db.Data.GatesOpenAccessExpirationDate;
            if (!canOpen)
            {
                await Arguments.Client.SendAsset(Arguments.Message, skin.Schema.Unavailable);
                await Arguments.Client.SendMessage(
                    Arguments.Message.Chat.Id,
                    "I cancelli sono bloccati al momento ❌",
                    disableNotification: true
                );
                return;
            }
            else
            {
                await OpenProcedure();
            }
        }
    }

    private async Task OpenProcedure()
    {
        AttachEvents();
        await Arguments
            .Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"Confermi di voler aprire il cancello {GateName}?",
                replyMarkup: new InlineKeyboardMarkup()
                    .AddButton("✅", EncodeCallbackQueryData("y"))
                    .AddButton("❌", EncodeCallbackQueryData("n")),
                disableNotification: true
            )
            .DeleteOnDetach(this);
    }

    protected override async Task OnCallback(Update update, string callbackData)
    {
        await Arguments.Client.AnswerCallbackQuery(update.CallbackQuery!.Id);

        if (callbackData == "y")
        {
            await Arguments.Client.SendChatAction(Arguments.Message.Chat.Id, ChatAction.Typing);
            await gateDriver.Open(gateId);
            await Arguments.Client.SendAsset(Arguments.Message, Asset);
            await Arguments.Client.SendMessage(
                Arguments.Message.Chat.Id,
                $"Ho aperto il cancello {GateName} 🐱",
                disableNotification: true
            );
            if (!adminIds.Contains(Arguments.Message.From!.Id))
            {
                await Arguments.Client.NotifyOwners(
                    $"🔑 <b>{Arguments.Message.From.Username}</b> ha aperto il cancello {GateName}"
                );
            }
        }

        await DetachEvents();
    }
}
