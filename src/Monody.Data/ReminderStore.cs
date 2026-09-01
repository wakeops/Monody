using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Monody.Data.Entities;

namespace Monody.Data;

public class ReminderStore
{
    /// <summary>Long enough to be useful, short enough that a typo can't schedule something in 3024.</summary>
    public static readonly TimeSpan MaxLeadTime = TimeSpan.FromDays(365);

    public static readonly TimeSpan MinLeadTime = TimeSpan.FromSeconds(30);

    private readonly IDbContextFactory<MonodyDbContext> _dbContextFactory;
    private readonly TimeProvider _timeProvider;

    public ReminderStore(IDbContextFactory<MonodyDbContext> dbContextFactory, TimeProvider timeProvider)
    {
        _dbContextFactory = dbContextFactory;
        _timeProvider = timeProvider;
    }

    public async Task<ReminderWriteResult> ScheduleAsync(ulong userId, ulong? channelId, string message, DateTimeOffset dueAt, CancellationToken cancellationToken = default)
    {
        var trimmed = message?.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return ReminderWriteResult.Rejected("A reminder needs something to say.");
        }

        if (trimmed.Length > DataConstants.MaxReminderLength)
        {
            return ReminderWriteResult.Rejected($"A reminder must be {DataConstants.MaxReminderLength} characters or fewer.");
        }

        var now = _timeProvider.GetUtcNow();

        if (dueAt - now < MinLeadTime)
        {
            return ReminderWriteResult.Rejected($"Reminders must be at least {MinLeadTime.TotalSeconds:N0} seconds away.");
        }

        if (dueAt - now > MaxLeadTime)
        {
            return ReminderWriteResult.Rejected($"Reminders cannot be more than {MaxLeadTime.TotalDays:N0} days away.");
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var pending = await db.Reminders.CountAsync(r => r.UserId == userId && r.DeliveredAt == null, cancellationToken);

        if (pending >= DataConstants.MaxPendingRemindersPerUser)
        {
            return ReminderWriteResult.Rejected(
                $"You already have {DataConstants.MaxPendingRemindersPerUser} reminders pending, which is the maximum.");
        }

        var reminder = new Reminder
        {
            UserId = userId,
            ChannelId = channelId,
            Message = trimmed,
            DueAt = dueAt,
            CreatedAt = now
        };

        db.Reminders.Add(reminder);
        await db.SaveChangesAsync(cancellationToken);

        return ReminderWriteResult.Scheduled(reminder);
    }

    public async Task<IReadOnlyList<Reminder>> GetPendingAsync(ulong userId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Reminders
            .Where(r => r.UserId == userId && r.DeliveredAt == null)
            .OrderBy(r => r.DueAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Reminders that are due and not yet delivered, oldest first.</summary>
    public async Task<IReadOnlyList<Reminder>> GetDueAsync(int limit, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Reminders
            .Where(r => r.DeliveredAt == null && r.DueAt <= now)
            .OrderBy(r => r.DueAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Marks a reminder delivered. Returns false when another pass got there first, so a
    /// reminder is not sent twice.
    /// </summary>
    public async Task<bool> MarkDeliveredAsync(int reminderId, CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var updated = await db.Reminders
            .Where(r => r.Id == reminderId && r.DeliveredAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.DeliveredAt, _timeProvider.GetUtcNow()), cancellationToken);

        return updated > 0;
    }

    public async Task<int> CancelAsync(ulong userId, IEnumerable<int> reminderIds, CancellationToken cancellationToken = default)
    {
        var ids = reminderIds?.Distinct().ToList() ?? [];

        if (ids.Count == 0)
        {
            return 0;
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        return await db.Reminders
            .Where(r => r.UserId == userId && r.DeliveredAt == null && ids.Contains(r.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }
}

public readonly record struct ReminderWriteResult(bool Success, Reminder Reminder, string Reason)
{
    public static ReminderWriteResult Scheduled(Reminder reminder) => new(true, reminder, null);

    public static ReminderWriteResult Rejected(string reason) => new(false, null, reason);
}
