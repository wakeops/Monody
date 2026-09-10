using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Monody.Data;

/// <summary>
/// SQLite has no date type, and EF refuses to order or compare a DateTimeOffset stored as text.
/// Everything here is UTC, so persist the instant as a number instead; that sorts and compares
/// correctly in SQL, which the reminder sweep depends on.
/// </summary>
internal static class InstantValueConverters
{
    public static readonly ValueConverter<DateTimeOffset, long> Instant = new(
        value => value.ToUnixTimeMilliseconds(),
        value => DateTimeOffset.FromUnixTimeMilliseconds(value));

    public static readonly ValueConverter<DateTimeOffset?, long?> NullableInstant = new(
        value => value.HasValue ? value.Value.ToUnixTimeMilliseconds() : null,
        value => value.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(value.Value) : null);
}
