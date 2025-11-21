using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Monody.OpenAI.Exceptions;
using Monody.OpenAI.ToolHandler;

namespace Monody.OpenAI.Services;

public sealed class ToolDispatcher
{
    private readonly IReadOnlyDictionary<string, IToolHandler> _handlers;
    private readonly ILogger _logger;

    public ToolDispatcher(IEnumerable<IToolHandler> handlers, ILogger<ToolDispatcher> logger)
    {
        _handlers = handlers.ToDictionary(h => h.Name, h => h);
        _logger = logger;
    }

    /// <summary>
    /// List of all tools with metadata for sending to OpenAI.
    /// </summary>
    public IReadOnlyList<OpenAIToolMetadata> GetAllMetadata()
        => [.. _handlers.Values.Select(h => h.ToMetadata())];

    /// <summary>
    /// Executes a tool by name with raw JSON arguments.
    /// </summary>
    public async Task<string> ExecuteAsync(
        string toolName,
        BinaryData arguments,
        CancellationToken cancellationToken = default)
    {
        if (!_handlers.TryGetValue(toolName, out var handler))
        {
            throw new InvalidOperationException($"No tool handler registered with name '{toolName}'.");
        }

        try
        {
            _logger.LogInformation("Executing tool: {ToolName}", toolName);

            var result = await handler.ExecuteAsync(arguments, cancellationToken);
            return result?.RootElement.GetRawText() ?? "{}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tool '{ToolName}' threw an exception", toolName);

            throw new ToolException(toolName, ex);
        }
    }
}
