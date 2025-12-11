using System;
using System.Threading;
using System.Threading.Tasks;
using Monody.AI.Domain.Abstractions;
using Monody.AI.Tools.ToolHandler;

namespace Monody.AI.Tools.Capabilities.ResearchAssistant;

internal class ResearchAssistantTool : ToolHandler<ResearchAssistantToolRequest, ResearchAssistantToolResponse>
{
    private readonly IResearchAgent _researchAgent;

    public ResearchAssistantTool(IResearchAgent researchAgent)
    {
        _researchAgent = researchAgent;
    }

    public override string Name => "research_assistant";

    public override string Description => "Use a specialized agent to investigate unknown or recent information";

    protected override async Task<ResearchAssistantToolResponse> HandleAsync(ResearchAssistantToolRequest request, CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Prompt))
        {
            throw new ArgumentNullException(nameof(request.Prompt));
        }

        var response = await _researchAgent.GetResultAsync(request.Prompt, cancellationToken);

        return new ResearchAssistantToolResponse
        {
            Response = response
        };
    }
}
