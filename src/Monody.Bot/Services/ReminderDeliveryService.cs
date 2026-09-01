using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Hosting;
using Discord.Addons.Hosting.Util;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using Monody.Bot.Modules;
using Monody.Data;
using Monody.Data.Entities;

namespace Monody.Bot.Services;

/// <summary>
/// Delivers reminders once they come due.
/// </summary>
/// <remarks>
/// Polls rather than scheduling timers: reminders live in SQLite and survive restarts, so the
/// only thing that has to survive a restart is this loop. A reminder is marked delivered before
/// the message is sent, and the update is conditional, so a slow send cannot produce a duplicate.
/// </remarks>
internal sealed class ReminderDeliveryService : DiscordClientService
{
    private static readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(30);

    private const int BatchSize = 25;

    private readonly ReminderStore _reminderStore;

    public ReminderDeliveryService(DiscordSocketClient client, ILogger<ReminderDeliveryService> logger, ReminderStore reminderStore)
        : base(client, logger)
    {
        _reminderStore = reminderStore;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Client.WaitForReadyAsync(stoppingToken);

        using var timer = new PeriodicTimer(_pollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DeliverDueAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                // A bad batch must not kill the loop; the next pass picks the work up again.
                Logger.LogError(ex, "Reminder delivery pass failed.");
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    return;
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task DeliverDueAsync(CancellationToken cancellationToken)
    {
        var due = await _reminderStore.GetDueAsync(BatchSize, cancellationToken);

        foreach (var reminder in due)
        {
            // Claim it first. If another pass already did, skip rather than send twice.
            if (!await _reminderStore.MarkDeliveredAsync(reminder.Id, cancellationToken))
            {
                continue;
            }

            try
            {
                await SendAsync(reminder);
            }
            catch (Exception ex)
            {
                // Deliberately stays marked delivered. Retrying a reminder whose channel is gone
                // or whose permissions changed would just loop forever.
                Logger.LogWarning(ex, "Could not deliver reminder {ReminderId} to channel {ChannelId}.", reminder.Id, reminder.ChannelId);
            }
        }
    }

    private async Task SendAsync(Reminder reminder)
    {
        var embed = new EmbedBuilder()
            .WithTitle("Reminder")
            .WithDescription(reminder.Message)
            .WithColor(new Color(MonodyConstants.DefaultEmbedColor))
            .WithFooter($"Set {reminder.CreatedAt:dd MMM yyyy HH:mm} UTC")
            .Build();

        if (reminder.ChannelId is ulong channelId && Client.GetChannel(channelId) is IMessageChannel channel)
        {
            await channel.SendMessageAsync($"<@{reminder.UserId}>", embed: embed);
            return;
        }

        // No usable channel - fall back to a DM so the reminder is not silently lost.
        var user = await Client.GetUserAsync(reminder.UserId);

        if (user is null)
        {
            Logger.LogWarning("Dropping reminder {ReminderId}: neither its channel nor its user is reachable.", reminder.Id);
            return;
        }

        await user.SendMessageAsync(embed: embed);
    }
}
