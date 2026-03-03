using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Monody.Bot.Modules.Slop.Utils;

public static class DiscordHelper
{
    public static void EnrichWithInteractionContext(ChatHistory history, ulong interactionId, IGuild guild, IChannel channel)
    {
        var parts = new[]
        {
            "[Context: data related to the initiating discord interaction.]",
            $"Discord Interaction: Id = '{interactionId}'",

            guild != null
                ? $"Discord Guild: Id = '{guild.Id}', Name = '{guild.Name}'"
                : "Discord Guild: unknown, you may not have sufficient permissions to access this data.",

            channel != null
                ? $"Discord Channel: Id = '{channel.Id}', Name = '{channel.Name}', Type = '{channel.GetChannelType()}'"
                : "Discord Channel: unknown, you may not have sufficient permissions to access this data.",
        };

        history.AddUserMessage(string.Join('\n', parts));
    }

    public static async Task EnrichWithMessageHistoryAsync(ChatHistory history, IMessageChannel channel, int lookbackCount)
    {
        if (lookbackCount == 0)
        {
            return;
        }

        var lines = await FetchRecentMessagesAsLinesAsync(channel, lookbackCount);
        if (lines.Count > 0)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[Context: last {lines.Count} message(s) from this channel]");
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }

            history.AddUserMessage($"Context:\n{sb}");
        }
    }

    private static async Task<List<string>> FetchRecentMessagesAsLinesAsync(IMessageChannel channel, int n)
    {
        var msgs = await channel.GetMessagesAsync(limit: n).FlattenAsync();

        var ordered = msgs
            .Where(m => m is not null && m.Type == MessageType.Default)
            .OrderBy(m => m.Timestamp)
            .ToList();

        var lines = new List<string>(ordered.Count);

        foreach (var m in ordered)
        {
            var author = (m.Author as SocketUser)?.GlobalName ?? m.Author.Username;

            var content = m.CleanContent?.Replace("\r", "")
                                         ?.Replace('\n', ' ')
                                         ?.Trim();

            if (string.IsNullOrEmpty(content))
            {
                if (m.Attachments?.Count > 0)
                {
                    content = $"[attachments: {m.Attachments.Count}]";
                }
                else
                {
                    continue;
                }
            }

            var t = m.Timestamp.ToLocalTime().ToString("HH:mm");

            lines.Add($"[{t}] {author}: {content}");
        }

        return lines;
    }
}
