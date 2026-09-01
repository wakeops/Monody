using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Monody.AI.Tools.Abstractions;
using Monody.Data;

namespace Monody.AI.Tools.Capabilities.Reminders;

/// <summary>
/// Schedules reminders delivered later by the bot. Like the memory tools, the user is taken
/// from the invocation context rather than from the model's arguments.
/// </summary>
public sealed class ReminderPlugin(ReminderStore reminderStore, IInvocationContext invocationContext, TimeProvider timeProvider)
{
    [KernelFunction("set_reminder")]
    [Description(
        "Schedules a reminder for the current user, delivered in this channel when it comes due. " +
        "Give either DelayMinutes for a relative time, or DueAtUtc for a specific clock time.")]
    public async Task<SetReminderToolResponse> SetReminderAsync(SetReminderToolRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Message);

        var userId = RequireUserId();

        if (ResolveDueAt(request) is not { } dueAt)
        {
            return new SetReminderToolResponse
            {
                Scheduled = false,
                Outcome = "Give either DelayMinutes above zero, or DueAtUtc as an ISO 8601 UTC timestamp."
            };
        }

        var result = await reminderStore.ScheduleAsync(userId, invocationContext.ChannelId, request.Message, dueAt, cancellationToken);

        return new SetReminderToolResponse
        {
            Scheduled = result.Success,
            DueAt = result.Success ? DiscordTimestamp(result.Reminder.DueAt) : null,
            Outcome = result.Success ? "Scheduled." : result.Reason
        };
    }

    [KernelFunction("list_reminders")]
    [Description("Lists the current user's pending reminders.")]
    public async Task<ListRemindersToolResponse> ListRemindersAsync(ListRemindersToolRequest request, CancellationToken cancellationToken = default)
    {
        var pending = await reminderStore.GetPendingAsync(RequireUserId(), cancellationToken);

        return new ListRemindersToolResponse
        {
            Reminders = [.. pending.Select(r => $"{DiscordTimestamp(r.DueAt)} — {r.Message}")]
        };
    }

    /// <summary>DelayMinutes wins when both are supplied; it is the far more common request.</summary>
    private DateTimeOffset? ResolveDueAt(SetReminderToolRequest request)
    {
        if (request.DelayMinutes > 0)
        {
            return timeProvider.GetUtcNow().AddMinutes(request.DelayMinutes);
        }

        if (!string.IsNullOrWhiteSpace(request.DueAtUtc) &&
            DateTimeOffset.TryParse(request.DueAtUtc.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private ulong RequireUserId() =>
        invocationContext.UserId
        ?? throw new InvalidOperationException("No Discord user is in scope, so reminders cannot be scheduled.");

    private static string DiscordTimestamp(DateTimeOffset moment) => $"<t:{moment.ToUnixTimeSeconds()}:R>";
}
