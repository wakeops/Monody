using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monody.AI.Provider.Exceptions;
using Monody.AI.Provider.Services.Abstractions;
using Monody.AI.Tools.ToolHandler;

namespace Monody.AI.Provider.Services;

public sealed class ToolDispatcher : IToolDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;
    private IReadOnlyDictionary<string, IToolHandler> _handlers;

    public ToolDispatcher(IServiceProvider serviceProvider, ILogger<ToolDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    private void EnsureHandlers()
    {
        if (_handlers != null)
        {
            return;
        }

        var handlers = _serviceProvider.GetServices<IToolHandler>();
        _handlers = handlers.ToDictionary(h => h.Name, h => h, StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<ToolMetadata> GetAllMetadata()
    {
        EnsureHandlers();

        return [.. _handlers.Values.Select(h => new ToolMetadata(h.Name, h.Description, h.Parameters))];
    }

    public async Task<string> ExecuteAsync(string toolName, BinaryData arguments, CancellationToken cancellationToken)
    {
        EnsureHandlers();

        if (!_handlers.TryGetValue(toolName, out var handler))
        {
            throw new InvalidOperationException($"No tool handler registered with name '{toolName}'.");
        }

        try
        {
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
