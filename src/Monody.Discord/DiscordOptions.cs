using System.ComponentModel.DataAnnotations;
using Discord;

namespace Monody.Discord;

internal sealed class DiscordOptions
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Discord:Token is required"), MinLength(10)]
    public string Token { get; set; } = string.Empty;

    public ulong? GuildId { get; set; }

    public GatewayIntents GatewayIntents { get; set; } = GatewayIntents.Guilds;

    public LogSeverity LogSeverity { get; set; } = LogSeverity.Info;
}
