using System.Collections.Concurrent;
using System.Collections.Generic;
using Monody.Bot.Modules.Slop.Models;

namespace Monody.Bot.Modules.Slop;

/// <summary>In-memory conversation history, keyed by the interaction that started the thread.</summary>
public class ConversationStore
{
    private readonly ConcurrentDictionary<ulong, DiscordConversation> _store = new();

    public void Save(ulong conversationId, DiscordConversation conversation)
        => _store[conversationId] = conversation;

    public DiscordConversation Get(ulong conversationId)
        => _store.GetValueOrDefault(conversationId);
}
