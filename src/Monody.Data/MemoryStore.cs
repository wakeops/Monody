using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Monody.Data.Entities;

namespace Monody.Data;

/// <summary>
/// Durable personal facts, scoped to one Discord user.
/// </summary>
/// <remarks>
/// Every method takes the user id as its first argument and filters on it. Nothing here lets a
/// caller reach another user's rows, which matters because the caller is ultimately the model.
/// </remarks>
public class MemoryStore
{
    // Categories that describe a single fact: remembering a new one replaces the old.
    private static readonly MemoryCategory[] _singleValued =
        [MemoryCategory.Name, MemoryCategory.Location, MemoryCategory.TimeZone];

    private readonly IDbContextFactory<MonodyDbContext> _dbContextFactory;
    private readonly TimeProvider _timeProvider;

    public MemoryStore(IDbContextFactory<MonodyDbContext> dbContextFactory, TimeProvider timeProvider)
    {
        _dbContextFactory = dbContextFactory;
        _timeProvider = timeProvider;
    }

    public async Task<IReadOnlyList<UserMemory>> GetAsync(ulong userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await db.UserMemories
            .Where(m => m.UserId == userId)
            .OrderBy(m => m.Category)
            .ThenBy(m => m.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Stores a fact, replacing the existing one for single-valued categories. Returns the
    /// outcome so the caller can tell the user what actually happened.
    /// </summary>
    public async Task<MemoryWriteResult> RememberAsync(ulong userId, MemoryCategory category, string content, CancellationToken cancellationToken = default)
    {
        var trimmed = content?.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return MemoryWriteResult.Rejected("A memory cannot be empty.");
        }

        if (trimmed.Length > DataConstants.MaxMemoryLength)
        {
            return MemoryWriteResult.Rejected($"A memory must be {DataConstants.MaxMemoryLength} characters or fewer.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.UserMemories
            .Where(m => m.UserId == userId)
            .ToListAsync(cancellationToken);

        if (existing.Any(m => m.Category == category && string.Equals(m.Content, trimmed, StringComparison.OrdinalIgnoreCase)))
        {
            return MemoryWriteResult.AlreadyKnown();
        }

        var replaced = false;

        if (_singleValued.Contains(category))
        {
            var superseded = existing.Where(m => m.Category == category).ToList();
            replaced = superseded.Count > 0;
            db.UserMemories.RemoveRange(superseded);
        }
        else if (existing.Count >= DataConstants.MaxMemoriesPerUser)
        {
            return MemoryWriteResult.Rejected(
                $"You already have the maximum of {DataConstants.MaxMemoriesPerUser} saved memories. " +
                "Remove one with /slop memories first.");
        }

        db.UserMemories.Add(new UserMemory
        {
            UserId = userId,
            Category = category,
            Content = trimmed,
            CreatedAt = _timeProvider.GetUtcNow()
        });

        await db.SaveChangesAsync(cancellationToken);

        return MemoryWriteResult.Saved(replaced);
    }

    /// <summary>Deletes the given memories, ignoring any id that is not this user's.</summary>
    public async Task<int> ForgetAsync(ulong userId, IEnumerable<int> memoryIds, CancellationToken cancellationToken = default)
    {
        var ids = memoryIds?.Distinct().ToList() ?? [];

        if (ids.Count == 0)
        {
            return 0;
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await db.UserMemories
            .Where(m => m.UserId == userId && ids.Contains(m.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<int> ForgetAllAsync(ulong userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await db.UserMemories
            .Where(m => m.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

public readonly record struct MemoryWriteResult(bool Success, bool Replaced, bool Duplicate, string Reason)
{
    public static MemoryWriteResult Saved(bool replaced) => new(true, replaced, false, null);

    public static MemoryWriteResult AlreadyKnown() => new(true, false, true, null);

    public static MemoryWriteResult Rejected(string reason) => new(false, false, false, reason);
}
