using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Monody.OpenAI.Tools.GetDiscordMessageHistory;

internal sealed class GetDiscordMessageHistoryRequest
{
    [Description("The ID of the channel that contains the messages.")]
    [Required]
    public ulong ChannelId { get; set; }

    [Description("The number of messages to look back between 1 and 100.")]
    [Required]
    public int MessageCount { get; set; }
}
