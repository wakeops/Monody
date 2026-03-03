using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Monody.AI.Tools.Abstractions;

namespace Monody.AI.Tools.Capabilities.ResearchAssistant;

public sealed class ResearchAssistantPlugin(IServiceProvider serviceProvider)
{
    [KernelFunction("research_assistant")]
    [Description("Use a specialized agent to investigate unknown or recent information")]
    public async Task<ResearchAssistantToolResponse> ResearchAsync(ResearchAssistantToolRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Prompt))
        {
            throw new ArgumentNullException(nameof(request.Prompt));
        }

        var researchAgent = serviceProvider.GetRequiredService<IResearchAgent>();

        var response = await researchAgent.GetResultAsync(request.Prompt, cancellationToken);

        return new ResearchAssistantToolResponse
        {
            Response = response
        };
    }
}
