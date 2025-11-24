using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Monody.OpenAI.ToolHandler;

namespace Monody.OpenAI.Tools.GetDiscordMessageHistory;

internal sealed class GetDiscordMessageHistoryHandler : ToolHandler<GetDiscordMessageHistoryRequest, GetDiscordMessageHistoryResponse>
{
    private readonly DiscordSocketClient _client;

    public GetDiscordMessageHistoryHandler(DiscordSocketClient client)
    {
        _client = client;
    }

    public override string Name => "get_discord_message_history";

    public override string Description => "For a Discord channel, retrieve a list of the last n number of messages.";

    protected override async Task<GetDiscordMessageHistoryResponse> HandleAsync(GetDiscordMessageHistoryRequest request, CancellationToken cancellationToken)
    {
        var channel = _client.GetChannel(request.ChannelId) as IMessageChannel
            ?? throw new InvalidOperationException($"Channel '{request.ChannelId}' was not found or is not a message channel.");

        // Fetch messages
        var messages = await channel.GetMessagesAsync(limit: request.MessageCount).FlattenAsync()
            ?? throw new InvalidOperationException($"Unable to get messages in channel '{request.ChannelId}'.");

        return new GetDiscordMessageHistoryResponse
        {
            ChannelId = channel.Id,
            ChannelType = channel.GetChannelType()?.ToString(),
            Messages = [ ..messages.Select(message => new GetDiscordMessageHistoryMessage {
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
