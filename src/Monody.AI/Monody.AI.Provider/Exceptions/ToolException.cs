using System;

namespace Monody.AI.Provider.Exceptions;

public class ToolException : Exception
{
    public string ToolName { get; }

    public ToolException(string toolName, Exception innerException)
        : base($"Tool execution failed for '{toolName}': {innerException.Message}", innerException)
    {
        ToolName = toolName;
    }
}
