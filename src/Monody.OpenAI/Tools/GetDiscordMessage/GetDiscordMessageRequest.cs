using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Monody.OpenAI.Tools.GetDiscordMessage;

internal sealed class GetDiscordMessageRequest
{
    [Description("The ID of the channel that contains the message.")]
    [Required]
    public ulong ChannelId { get; set; }

    [Description("The ID of the message to fetch.")]
    [Required]
    public ulong MessageId { get; set; }

    [Description("Optional guild ID if the message is in a guild.")]
    public ulong? GuildId { get; set; }
}
