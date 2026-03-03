using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Monody.AI.Tools.Abstractions;

namespace Monody.AI.Tools.Capabilities.ResearchAssistant;

public sealed class ResearchAssistantPlugin(IResearchAgent researchAgent)
{
    [KernelFunction("research_assistant")]
    [Description("Use a specialized agent to investigate unknown or recent information")]
    public async Task<ResearchAssistantToolResponse> ResearchAsync(ResearchAssistantToolRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Prompt))
        {
            throw new ArgumentNullException(nameof(request.Prompt));
        }

        var response = await researchAgent.GetResultAsync(request.Prompt, cancellationToken);

        return new ResearchAssistantToolResponse
        {
            Response = response
        };
    }
}
