using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.SemanticKernel;

namespace Monody.AI.Tools.Capabilities.GetDiscordMessage;

public sealed class GetDiscordMessagePlugin(DiscordSocketClient client)
{
    [KernelFunction("get_discord_message")]
    [Description("Retrieves a Discord message and returns contextual information such as guild, channel, author, and content.")]
    public async Task<GetDiscordMessageResponse> GetMessageAsync(GetDiscordMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (client.GetChannel(request.ChannelId) is not IMessageChannel channel)
        {
            throw new InvalidOperationException($"Channel '{request.ChannelId}' was not found or is not a message channel.");
        }

        var message = await channel.GetMessageAsync(request.MessageId)
            ?? throw new InvalidOperationException($"Message '{request.MessageId}' was not found in channel '{request.ChannelId}'.");

        var author = message.Author;

        return new GetDiscordMessageResponse
        {
            ChannelId = channel.Id,
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
