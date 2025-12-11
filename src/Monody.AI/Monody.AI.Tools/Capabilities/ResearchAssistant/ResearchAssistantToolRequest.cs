using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Monody.AI.Tools.Capabilities.ResearchAssistant;

internal sealed class ResearchAssistantToolRequest
{
    [Description("The prompt to send to the assistant agent")]
    [Required]
    public string Prompt { get; set; }
}
