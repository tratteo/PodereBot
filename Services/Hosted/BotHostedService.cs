﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PodereBot.Lib;
using PodereBot.Lib.Commands;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace PodereBot.Services.Hosted;

internal class BotHostedService : IHostedService
{
    private readonly ILogger logger;
    private readonly IServiceProvider services;
    private readonly CancellationTokenSource cancellationToken = new CancellationTokenSource();
    private readonly TelegramBotClient client;
    private readonly GateDriver gate;

    public BotHostedService(
        ILogger<BotHostedService> logger,
        IConfiguration configuration,
        IServiceProvider services,
        GateDriver gate
    )
    {
        this.gate = gate;
        this.logger = logger;
        this.services = services;
        client = new TelegramBotClient(
            configuration.GetValue<string>("TELEGRAM_API_KEY")!,
            cancellationToken: cancellationToken.Token
        );
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var me = await client.GetMeAsync(cancellationToken: cancellationToken);
        await client.SetMyCommandsAsync(
            Registry.commands.ConvertAll(c => new BotCommand() { Command = c.command, Description = c.description }),
            cancellationToken: cancellationToken
        );
        client.OnMessage += OnMessage;
        logger.LogInformation("@{u} is running", me.Username);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        this.cancellationToken.Cancel();
        await Task.CompletedTask;
    }

    private async Task OnMessage(Message msg, UpdateType type)
    {
        logger.LogInformation("Received {type} '{t}' in {c}", type, msg.Text, msg.Chat);

        var match = Registry.commands.FirstOrDefault(c => c.command == msg.Text);
        if (match == null)
        {
            return;
        }
        var cmdService = (Command?)services.GetService(match.commandType);
        if (cmdService == null)
        {
            logger.LogWarning("unable to retrieve command service of type {t}", match.commandType);
            return;
        }
        try
        {
            await cmdService.Execute(new CommandArguments()
            {
                Client = client,
                Message = msg,
                Admin = match.admin
            });
        }
        catch (Exception ex)
        {
            logger.LogError("error executing command [{c}]: {e}", match.command, ex);
        }
    }
}