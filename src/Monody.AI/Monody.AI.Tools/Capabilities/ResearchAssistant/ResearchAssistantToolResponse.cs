using System.ComponentModel;

namespace Monody.AI.Tools.Capabilities.ResearchAssistant;

public sealed class ResearchAssistantToolResponse
{
    [Description("The results of the prompt")]
    public string Response { get; set; }
}
