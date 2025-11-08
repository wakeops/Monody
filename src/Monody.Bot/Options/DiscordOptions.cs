using System.ComponentModel.DataAnnotations;
using Discord;

namespace Monody.Bot.Options;

internal sealed class DiscordOptions
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Discord:Token is required"), MinLength(10)]
    public string Token { get; set; } = string.Empty;

    public ulong? GuildId { get; set; }

    public ulong? OwnerId { get; set; }

    public GatewayIntents GatewayIntents { get; set; } =
        GatewayIntents.GuildMessages |
        GatewayIntents.MessageContent |
        GatewayIntents.GuildMembers;

    public LogSeverity LogSeverity { get; set; } = LogSeverity.Info;
}
