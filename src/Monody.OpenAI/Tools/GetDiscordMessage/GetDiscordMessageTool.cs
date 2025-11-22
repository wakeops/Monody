using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Monody.OpenAI.ToolHandler;

namespace Monody.OpenAI.Tools.GetDiscordMessage;

internal sealed class GetDiscordMessageHandler : ToolHandler<GetDiscordMessageRequest, GetDiscordMessageResponse>
{
    private readonly DiscordSocketClient _client;

    public GetDiscordMessageHandler(DiscordSocketClient client)
    {
        _client = client;
    }

    public override string Name => "get_discord_message";

    public override string Description => "Retrieves a Discord message and returns contextual information such as guild, channel, author, and content.";

    protected override async Task<GetDiscordMessageResponse> HandleAsync(GetDiscordMessageRequest request, CancellationToken cancellationToken)
    {
        IGuild guild = null;
        if (request.GuildId.HasValue)
        {
            guild = _client.GetGuild(request.GuildId.Value);
        }

        // Resolve channel
        if (_client.GetChannel(request.ChannelId) is not IMessageChannel channel)
        {
            throw new InvalidOperationException($"Channel '{request.ChannelId}' was not found or is not a message channel.");
        }

        // Fetch message
        var message = await channel.GetMessageAsync(request.MessageId)
            ?? throw new InvalidOperationException($"Message '{request.MessageId}' was not found in channel '{request.ChannelId}'.");

        var author = message.Author;

        return new GetDiscordMessageResponse
        {
            GuildId = guild?.Id,
            GuildName = guild?.Name,

            ChannelId = channel.Id,
            ChannelName = channel.Name,
            ChannelType = channel.GetChannelType().ToString(),

            MessageId = message.Id,
            Content = message.Content ?? "",
            Timestamp = message.Timestamp,

            AuthorId = author.Id,
            AuthorUsername = author.Username,
            AuthorGlobalName = author.GlobalName,

            Attachments = [.. message.Attachments.Select(a => a.Url)],
            Embeds = [.. message.Embeds.Select(e => e.Title ?? "(embed)")]
        };
    }
}
