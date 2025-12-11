using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Monody.AI.Tools.ToolHandler;

namespace Monody.AI.Provider.Services.Abstractions;

public interface IToolDispatcher
{
    /// <summary>
    /// List of all tools with metadata.
    /// </summary>
    IReadOnlyList<ToolMetadata> GetAllMetadata();

    /// <summary>
    /// Executes a tool by name with raw JSON arguments.
    /// </summary>
    Task<string> ExecuteAsync(string toolName, BinaryData arguments, CancellationToken cancellationToken);
}
