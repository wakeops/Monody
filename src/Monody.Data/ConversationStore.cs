using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Monody.Data.Entities;

namespace Monody.Data;

/// <summary>
/// Durable storage for /slop conversations.
/// </summary>
/// <remarks>
/// Only the user and assistant turns are kept. Tool calls and their results are dropped once a
/// round finishes: they are needed to complete that round, not to carry the thread forward, and
/// keeping them would mean serialising Semantic Kernel's polymorphic content types.
/// </remarks>
public class ConversationStore
{
    /// <summary>Most recent turns kept per conversation, so a long thread cannot grow forever.</summary>
    public const int MaxTurns = 40;

    /// <summary>Conversations older than this are swept away; nobody follows up on a month-old thread.</summary>
    public static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(30);

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly IDbContextFactory<MonodyDbContext> _dbContextFactory;
    private readonly TimeProvider _timeProvider;

    public ConversationStore(IDbContextFactory<MonodyDbContext> dbContextFactory, TimeProvider timeProvider)
    {
        _dbContextFactory = dbContextFactory;
        _timeProvider = timeProvider;
    }

    public async Task<bool> ExistsAsync(ulong conversationId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Conversations.AnyAsync(c => c.Id == conversationId, cancellationToken);
    }

    /// <summary>Returns the stored turns, or null when there is no such conversation.</summary>
    public async Task<IReadOnlyList<ConversationTurn>> GetTurnsAsync(ulong conversationId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var stored = await db.Conversations
            .Where(c => c.Id == conversationId)
            .Select(c => c.TurnsJson)
            .FirstOrDefaultAsync(cancellationToken);

        if (stored is null)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<List<ConversationTurn>>(stored, _jsonOptions) ?? [];
        }
        catch (JsonException)
        {
            // A row written by an older shape should start the thread over, not break the command.
            return [];
        }
    }

    public async Task SaveAsync(ulong conversationId, ulong userId, ulong? channelId, ulong? guildId, IEnumerable<ConversationTurn> turns, CancellationToken cancellationToken = default)
    {
        var kept = turns?.TakeLast(MaxTurns).ToList() ?? [];
        var now = _timeProvider.GetUtcNow();

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var conversation = await db.Conversations.FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation is null)
        {
            conversation = new Conversation { Id = conversationId, CreatedAt = now };
            db.Conversations.Add(conversation);
        }

        conversation.UserId = userId;
        conversation.ChannelId = channelId;
        conversation.GuildId = guildId;
        conversation.TurnsJson = JsonSerializer.Serialize(kept, _jsonOptions);
        conversation.UpdatedAt = now;

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Deletes conversations past the retention window. Returns how many went.</summary>
    public async Task<int> PruneAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = _timeProvider.GetUtcNow() - RetentionPeriod;

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Conversations
            .Where(c => c.UpdatedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
