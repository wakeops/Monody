using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Monody.OpenAI.ToolHandler;

public abstract class ToolHandler<TRequest, TResponse> : IToolHandler
{
    private readonly JsonSerializerOptions _serializerOptions;

    protected ToolHandler(JsonSerializerOptions serializerOptions = null)
    {
        _serializerOptions = serializerOptions ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        ParametersSchema = JsonSchemaBuilder.FromType<TRequest>();
    }

    public abstract string Name { get; }

    public abstract string Description { get; }

    public JsonDocument ParametersSchema { get; }

    public OpenAIToolMetadata ToMetadata()
        => new(Name, Description, ParametersSchema);

    public async Task<JsonDocument> ExecuteAsync(BinaryData arguments, CancellationToken cancellationToken = default)
    {
        var request = JsonSerializer.Deserialize<TRequest>(arguments, _serializerOptions)
            ?? throw new InvalidOperationException($"Failed to deserialize arguments for tool '{Name}' to {typeof(TRequest).Name}.");

        var response = await HandleAsync(request, cancellationToken);

        if (response is null)
        {
            return null;
        }

        var json = JsonSerializer.SerializeToElement(response, _serializerOptions);

        return JsonDocument.Parse(json.GetRawText());
    }

    protected abstract Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}
