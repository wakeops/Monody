using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using OpenAI.Chat;

namespace Monody.Module.AIChat.Utils;

public static class DiscordHelper
{
    public static async Task AddContextBlockAsync(IMessageChannel channel, int lookbackCount, List<ChatMessage> messageList)
    {
        // Build optional channel context
        string contextBlock = null;

        var lines = await FetchRecentMessagesAsLinesAsync(channel, lookbackCount);
        if (lines.Count > 0)
        {
            // Keep the context compact to avoid token bloat
            var sb = new StringBuilder();
            sb.AppendLine($"[Context: last {lines.Count} message(s) from this channel]");
            foreach (var line in lines)
            {
                sb.AppendLine(line);
            }

            contextBlock = sb.ToString();
        }

        if (!string.IsNullOrEmpty(contextBlock))
        {
            messageList.Add(new UserChatMessage($"Context:\n{contextBlock}"));
        }
    }

    private static async Task<List<string>> FetchRecentMessagesAsLinesAsync(IMessageChannel channel, int n)
    {
        // GetMessagesAsync returns newest→older; we’ll sort ascending afterward.
        var msgs = await channel.GetMessagesAsync(limit: n).FlattenAsync();

        // Oldest → newest for better reading
        var ordered = msgs
            .Where(m => m is not null && m.Type == MessageType.Default) // skip joins/pins/etc
            .OrderBy(m => m.Timestamp)
            .ToList();

        var lines = new List<string>(ordered.Count);

        foreach (var m in ordered)
        {
            // author display: prefer global name, then username
            var author = (m.Author as SocketUser)?.GlobalName ?? m.Author.Username;

            // collapse whitespace; keep the first line reasonably short
            var content = m.CleanContent?.Replace("\r", "")
                                         ?.Replace('\n', ' ')
                                         ?.Trim();

            if (string.IsNullOrEmpty(content))
            {
                // If the message has only attachments, note that briefly.
                if (m.Attachments?.Count > 0)
                {
                    content = $"[attachments: {m.Attachments.Count}]";
                }
                else
                {
                    continue; // skip empty
                }
            }

            // timestamp in HH:mm (local to the bot host)
            var t = m.Timestamp.ToLocalTime().ToString("HH:mm");

            lines.Add($"[{t}] {author}: {content}");
        }

        return lines;
    }
}
