using System.Collections.Concurrent;
using Monody.Module.AIChat.Models;

namespace Monody.Module.AIChat;

public class ConversationStore
{
    private readonly ConcurrentDictionary<string, DiscordConversation> _store = new ();

    public void SaveConversation(string conversationId, DiscordConversation conversation)
        => _store[conversationId] = conversation;

    public DiscordConversation GetConversation(string conversationId)
        => _store.TryGetValue(conversationId, out var conversation) ? conversation : null;
}
