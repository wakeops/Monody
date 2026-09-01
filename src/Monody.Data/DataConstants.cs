namespace Monody.Data;

public static class DataConstants
{
    public const int MaxMemoryLength = 200;

    /// <summary>Per user. Keeps the store small enough to inject wholesale into a prompt.</summary>
    public const int MaxMemoriesPerUser = 25;

    public const int MaxReminderLength = 500;

    public const int MaxPendingRemindersPerUser = 10;
}
