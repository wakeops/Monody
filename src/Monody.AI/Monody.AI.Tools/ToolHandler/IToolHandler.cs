using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Monody.AI.Tools.ToolHandler;

public interface IToolHandler
{
    /// <summary>Tool name as seen by the model.</summary>
    string Name { get; }

    /// <summary>Human-readable description.</summary>
    string Description { get; }

    /// <summary>Schema describing the tool parameters.</summary>
    List<ToolParameterSchema> Parameters { get; }

    /// <summary>
    /// Executes the tool from raw JSON arguments and returns a JSON result.
    /// </summary>
    Task<JsonDocument> ExecuteAsync(BinaryData arguments, CancellationToken cancellationToken = default);
}
