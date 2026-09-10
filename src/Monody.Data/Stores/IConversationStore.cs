using Monody.Data.Entities;

namespace Monody.Data.Stores;

public interface IConversationStore
{
    Task<bool> ExistsAsync(ulong conversationId, CancellationToken cancellationToken = default);

    /// <summary>Returns the stored turns, or null when there is no such conversation.</summary>
    Task<IReadOnlyList<ConversationTurn>> GetTurnsAsync(ulong conversationId, CancellationToken cancellationToken = default);

    Task SaveAsync(ulong conversationId, ulong userId, ulong? channelId, ulong? guildId, IEnumerable<ConversationTurn> turns, CancellationToken cancellationToken = default);

    /// <summary>Deletes conversations past the retention window. Returns how many went.</summary>
    Task<int> PruneAsync(CancellationToken cancellationToken = default);
}
