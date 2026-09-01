using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Time.Testing;
using Xunit;

namespace Monody.Data.Tests;

public class ReminderStoreTests : IDisposable
{
    private const ulong Alice = 111;
    private const ulong Bob = 222;
    private const ulong Channel = 999;

    private static readonly DateTimeOffset _now = new(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly SqliteFixture _fixture = new();
    private readonly FakeTimeProvider _time = new(_now);
    private readonly ReminderStore _store;

    public ReminderStoreTests()
    {
        _store = new ReminderStore(_fixture.CreateFactory(), _time);
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task SchedulesAReminder()
    {
        var result = await _store.ScheduleAsync(Alice, Channel, "Check the deploy", _now.AddHours(2));

        Assert.True(result.Success);
        Assert.Equal(_now.AddHours(2), result.Reminder.DueAt);
        Assert.Single(await _store.GetPendingAsync(Alice));
    }

    [Fact]
    public async Task RejectsSomethingAlreadyDue()
    {
        var result = await _store.ScheduleAsync(Alice, Channel, "Too soon", _now.AddSeconds(5));

        Assert.False(result.Success);
        Assert.Empty(await _store.GetPendingAsync(Alice));
    }

    [Fact]
    public async Task RejectsSomethingAbsurdlyFarOut()
    {
        var result = await _store.ScheduleAsync(Alice, Channel, "In the year 3000", _now.AddYears(5));

        Assert.False(result.Success);
        Assert.Contains("days", result.Reason);
    }

    [Fact]
    public async Task CapsPendingRemindersPerUser()
    {
        for (var i = 0; i < DataConstants.MaxPendingRemindersPerUser; i++)
        {
            Assert.True((await _store.ScheduleAsync(Alice, Channel, $"Reminder {i}", _now.AddHours(i + 1))).Success);
        }

        Assert.False((await _store.ScheduleAsync(Alice, Channel, "One too many", _now.AddHours(50))).Success);

        // Another user is unaffected by Alice hitting her cap.
        Assert.True((await _store.ScheduleAsync(Bob, Channel, "Bob's first", _now.AddHours(1))).Success);
    }

    [Fact]
    public async Task DeliveredRemindersFreeUpTheCap()
    {
        for (var i = 0; i < DataConstants.MaxPendingRemindersPerUser; i++)
        {
            await _store.ScheduleAsync(Alice, Channel, $"Reminder {i}", _now.AddHours(i + 1));
        }

        var first = (await _store.GetPendingAsync(Alice)).First();
        await _store.MarkDeliveredAsync(first.Id);

        Assert.True((await _store.ScheduleAsync(Alice, Channel, "Room for one more", _now.AddHours(50))).Success);
    }

    [Fact]
    public async Task ReturnsOnlyRemindersThatAreDue()
    {
        await _store.ScheduleAsync(Alice, Channel, "Soon", _now.AddMinutes(10));
        await _store.ScheduleAsync(Alice, Channel, "Later", _now.AddHours(5));

        Assert.Empty(await _store.GetDueAsync(10));

        _time.Advance(TimeSpan.FromMinutes(11));

        Assert.Equal("Soon", (await _store.GetDueAsync(10)).Single().Message);
    }

    [Fact]
    public async Task MarkDeliveredIsClaimedOnlyOnce()
    {
        // Two delivery passes overlapping must not send the same reminder twice.
        await _store.ScheduleAsync(Alice, Channel, "Check the deploy", _now.AddMinutes(1));
        _time.Advance(TimeSpan.FromMinutes(2));

        var due = (await _store.GetDueAsync(10)).Single();

        Assert.True(await _store.MarkDeliveredAsync(due.Id));
        Assert.False(await _store.MarkDeliveredAsync(due.Id));
        Assert.Empty(await _store.GetDueAsync(10));
    }

    [Fact]
    public async Task WillNotCancelAnotherUsersReminder()
    {
        await _store.ScheduleAsync(Bob, Channel, "Bob's reminder", _now.AddHours(1));
        var bobsId = (await _store.GetPendingAsync(Bob)).Single().Id;

        Assert.Equal(0, await _store.CancelAsync(Alice, [bobsId]));
        Assert.Single(await _store.GetPendingAsync(Bob));
    }
}
