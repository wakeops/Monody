using Monody.Data.Entities;

namespace Monody.Data.Stores;

public interface IMemoryStore
{
    Task<IReadOnlyList<UserMemory>> GetAsync(ulong userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores a fact, replacing the existing one for single-valued categories. Returns the
    /// outcome so the caller can tell the user what actually happened.
    /// </summary>
    Task<MemoryWriteResult> RememberAsync(ulong userId, MemoryCategory category, string content, CancellationToken cancellationToken = default);

    /// <summary>Deletes the given memories, ignoring any id that is not this user's.</summary>
    Task<int> ForgetAsync(ulong userId, IEnumerable<int> memoryIds, CancellationToken cancellationToken = default);

    Task<int> ForgetAllAsync(ulong userId, CancellationToken cancellationToken = default);
}
