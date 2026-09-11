namespace Monody.Services.Graylog;

/// <summary>
/// Deliberately not [Required]: Graylog is an optional integration. The tool checks
/// <see cref="GraylogService.IsConfigured"/> before acting rather than the bot refusing to start
/// when nobody has set it up.
/// </summary>
public sealed class GraylogOptions
{
    public string BaseUrl { get; set; } = string.Empty;

    public string ApiKey { get; set; } = string.Empty;
}
