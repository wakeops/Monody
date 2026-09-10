using Monody.Data.Entities;

namespace Monody.Data.Stores;

public interface IMemoryStore
{
    Task<IReadOnlyList<UserMemory>> GetAsync(ulong userId, CancellationToken cancellationToken = default);

    Task<MemoryWriteResult> RememberAsync(ulong userId, MemoryCategory category, string content, CancellationToken cancellationToken = default);

    Task<int> ForgetAsync(ulong userId, IEnumerable<int> memoryIds, CancellationToken cancellationToken = default);

    Task<int> ForgetAllAsync(ulong userId, CancellationToken cancellationToken = default);
}
