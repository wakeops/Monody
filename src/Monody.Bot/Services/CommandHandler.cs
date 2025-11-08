using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord.Addons.Hosting;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;

namespace Monody.Bot.Services;

internal class CommandHandler : DiscordClientService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly CommandService _commandService;

    public CommandHandler(DiscordSocketClient client, ILogger<DiscordClientService> logger, IServiceProvider provider, CommandService commandService) : base(client, logger)
    {
        _serviceProvider = provider;
        _commandService = commandService;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        Client.MessageReceived += HandleCommandAsync;

        await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);
    }

    private async Task HandleCommandAsync(SocketMessage socketMessage)
    {
        try
        {
            int argPos = 0;

            var message = socketMessage as SocketUserMessage;
            if (message == null || message.Author.IsBot || !message.HasMentionPrefix(Client.CurrentUser, ref argPos))
            {
                return;
            }

            var context = new SocketCommandContext(Client, message);

            var result = await _commandService.ExecuteAsync(context, argPos, _serviceProvider);

            if (!result.IsSuccess && result.Error != CommandError.UnknownCommand)
            {
                Logger.Log_CommandFailure(result.Error, result.ErrorReason);

                await context.Channel.SendMessageAsync("Something went wrong processing this request.");
            }
        }
        catch (Exception ex)
        {
            Logger.Log_CommandException(ex);
        }
    }
}

internal static partial class CommandHandlerLoggingExtensions
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to handle command: {Error}: {ErrorReason}")]
    public static partial void Log_CommandFailure(this ILogger logger, CommandError? Error, string ErrorReason);

    [LoggerMessage(Level = LogLevel.Error, Message = "Exception occurred whilst attempting to handle command.")]
    public static partial void Log_CommandException(this ILogger logger, Exception ex);
}
