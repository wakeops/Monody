using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Monody.Data.Entities;

namespace Monody.Data.Stores;

public class MemoryStore : IMemoryStore
{
    private static readonly Regex _slugPattern = new("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.Compiled);

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
            .OrderBy(m => m.Slug)
            .ToListAsync(cancellationToken);
    }

    public Task<IReadOnlyList<UserMemory>> GetIndexAsync(ulong userId, CancellationToken cancellationToken = default) =>
        GetAsync(userId, cancellationToken);

    public async Task<UserMemory> GetTopicAsync(ulong userId, string slug, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug?.Trim().ToLowerInvariant();

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await db.UserMemories
            .FirstOrDefaultAsync(m => m.UserId == userId && m.Slug == normalizedSlug, cancellationToken);
    }

    public async Task<MemoryWriteResult> RememberAsync(ulong userId, string slug, string description, string content, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalizedSlug) || normalizedSlug.Length > DataConstants.MaxSlugLength || !_slugPattern.IsMatch(normalizedSlug))
        {
            return MemoryWriteResult.Rejected(
                $"Slug must be lowercase kebab-case, e.g. 'home-location', and {DataConstants.MaxSlugLength} characters or fewer.");
        }

        var trimmedDescription = description?.Trim();

        if (string.IsNullOrWhiteSpace(trimmedDescription) || trimmedDescription.Length > DataConstants.MaxMemoryDescriptionLength)
        {
            return MemoryWriteResult.Rejected($"Description must be non-empty and {DataConstants.MaxMemoryDescriptionLength} characters or fewer.");
        }

        var trimmedContent = content?.Trim();

        if (string.IsNullOrWhiteSpace(trimmedContent) || trimmedContent.Length > DataConstants.MaxMemoryContentLength)
        {
            return MemoryWriteResult.Rejected($"Content must be non-empty and {DataConstants.MaxMemoryContentLength} characters or fewer.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var existing = await db.UserMemories
            .Where(m => m.UserId == userId)
            .ToListAsync(cancellationToken);

        var match = existing.FirstOrDefault(m => string.Equals(m.Slug, normalizedSlug, StringComparison.Ordinal));

        if (match is not null)
        {
            if (string.Equals(match.Description, trimmedDescription, StringComparison.OrdinalIgnoreCase)
                && string.Equals(match.Content, trimmedContent, StringComparison.OrdinalIgnoreCase))
            {
                return MemoryWriteResult.AlreadyKnown();
            }

            match.Description = trimmedDescription;
            match.Content = trimmedContent;
            match.UpdatedAt = _timeProvider.GetUtcNow();

            await db.SaveChangesAsync(cancellationToken);

            return MemoryWriteResult.Saved(replaced: true);
        }

        if (existing.Count >= DataConstants.MaxMemoriesPerUser)
        {
            return MemoryWriteResult.Rejected(
                $"You already have the maximum of {DataConstants.MaxMemoriesPerUser} saved topics. " +
                "Remove one with /slop memories first.");
        }

        var now = _timeProvider.GetUtcNow();

        db.UserMemories.Add(new UserMemory
        {
            UserId = userId,
            Slug = normalizedSlug,
            Description = trimmedDescription,
            Content = trimmedContent,
            CreatedAt = now,
            UpdatedAt = now
        });

        await db.SaveChangesAsync(cancellationToken);

        return MemoryWriteResult.Saved(replaced: false);
    }

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
