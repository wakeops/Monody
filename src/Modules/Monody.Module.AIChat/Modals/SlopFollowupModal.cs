using Discord;
using Discord.Interactions;

namespace Monody.Module.AIChat.Modals;

public class SlopFollowupModal : IModal
{
    public string Title => "Monody follow up";

    [InputLabel("Prompt")]
    [ModalTextInput("prompt", TextInputStyle.Paragraph,
        placeholder: "Ask a follow up...", maxLength: 2000)]
    public string FollowupText { get; set; } = string.Empty;
}
