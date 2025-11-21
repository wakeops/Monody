using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Monody.OpenAI.ToolHandler;

public interface IToolHandler
{
    /// <summary>Tool name as seen by the model.</summary>
    string Name { get; }

    /// <summary>Human-readable description.</summary>
    string Description { get; }

    /// <summary>JSON Schema describing the tool parameters.</summary>
    JsonDocument ParametersSchema { get; }

    /// <summary>
    /// Executes the tool from raw JSON arguments and returns a JSON result.
    /// </summary>
    Task<JsonDocument> ExecuteAsync(BinaryData arguments, CancellationToken cancellationToken = default);

    /// <summary>Metadata you can send to OpenAI.</summary>
    OpenAIToolMetadata ToMetadata();
}
