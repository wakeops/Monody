using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Monody.Module.AIChat.Tools.Abstractions;

namespace Monody.Module.AIChat.Tools;

public class ChatToolProvider
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger _logger;

    public ChatToolProvider(IServiceProvider serviceProvider, ILogger<ChatToolProvider> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public ChatToolRunner GetChatTool(string toolName)
    {
        var tool = _serviceProvider.GetKeyedService<ChatToolRunner>(toolName);

        if (tool == null)
        {
            _logger.LogWarning("Unable to find requested chat tool: {ToolName}.", toolName);
            return null;
        }

        _logger.LogDebug("Found requested chat tool for '{ToolName}': {ToolType}", toolName, tool.GetType().FullName);

        return tool;
    }
}
