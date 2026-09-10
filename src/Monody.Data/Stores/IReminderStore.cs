using Monody.Data.Entities;

namespace Monody.Data.Stores;

public interface IReminderStore
{
    Task<ReminderWriteResult> ScheduleAsync(ulong userId, ulong? channelId, string message, DateTimeOffset dueAt, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Reminder>> GetPendingAsync(ulong userId, CancellationToken cancellationToken = default);

    /// <summary>Reminders that are due and not yet delivered, oldest first.</summary>
    Task<IReadOnlyList<Reminder>> GetDueAsync(int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a reminder delivered. Returns false when another pass got there first, so a
    /// reminder is not sent twice.
    /// </summary>
    Task<bool> MarkDeliveredAsync(int reminderId, CancellationToken cancellationToken = default);

    Task<int> CancelAsync(ulong userId, IEnumerable<int> reminderIds, CancellationToken cancellationToken = default);
}
