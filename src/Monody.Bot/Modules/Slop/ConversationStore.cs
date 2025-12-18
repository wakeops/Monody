using System.Collections.Concurrent;
using Monody.Bot.Modules.Slop.Models;

namespace Monody.Bot.Modules.Slop;

public class ConversationStore
{
    private readonly ConcurrentDictionary<string, DiscordConversation> _store = new ();

    public void SaveConversation(string conversationId, DiscordConversation conversation)
        => _store[conversationId] = conversation;

    public DiscordConversation GetConversation(string conversationId)
        => _store.TryGetValue(conversationId, out var conversation) ? conversation : null;
}
