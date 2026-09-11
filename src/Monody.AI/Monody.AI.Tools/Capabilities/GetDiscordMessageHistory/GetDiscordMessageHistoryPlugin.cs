using System.ComponentModel;
using Discord;
using Discord.WebSocket;
using Microsoft.SemanticKernel;
using Monody.AI.Tools.Abstractions;

namespace Monody.AI.Tools.Capabilities.GetDiscordMessageHistory;

public sealed class GetDiscordMessageHistoryPlugin(DiscordSocketClient client, IInvocationContext invocationContext)
{
    [KernelFunction("get_discord_message_history")]
    [Description(
        "For a Discord channel, retrieve a list of the last n number of messages. Only available " +
        "when Monody is installed to the server, not when running only as the current user's " +
        "personal app.")]
    public async Task<GetDiscordMessageHistoryResponse> GetHistoryAsync(GetDiscordMessageHistoryRequest request, CancellationToken cancellationToken = default)
    {
        invocationContext.EnsureIsGuildInstall();
        invocationContext.EnsureCanReadMessages();

        var channel = client.GetChannel(request.ChannelId) as IMessageChannel
            ?? throw new InvalidOperationException($"Channel '{request.ChannelId}' was not found or is not a message channel.");

        var messages = await channel.GetMessagesAsync(limit: request.MessageCount).FlattenAsync()
            ?? throw new InvalidOperationException($"Unable to get messages in channel '{request.ChannelId}'.");

        return new GetDiscordMessageHistoryResponse
        {
            ChannelId = channel.Id,
            ChannelType = channel.GetChannelType()?.ToString(),
            Messages = [.. messages.Select(message => new GetDiscordMessageHistoryMessage
            {
                MessageId = message.Id,
                Content = message.Content ?? "",
                Timestamp = message.Timestamp,
                AuthorId = message.Author.Id,
                AuthorUsername = message.Author.Username,
                AuthorGlobalName = message.Author.GlobalName,
                Attachments = [.. message.Attachments.Select(a => a.Url)],
                Embeds = [.. message.Embeds.Select(e => e.Title ?? "(embed)")]
            })]
        };
    }
}
