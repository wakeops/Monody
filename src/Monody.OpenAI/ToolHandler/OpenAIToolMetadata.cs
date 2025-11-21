using System.Text.Json;

namespace Monody.OpenAI.ToolHandler;

public sealed class OpenAIToolMetadata
{
    public string Name { get; }

    public string Description { get; }

    public JsonDocument ParametersSchema { get; }

    public OpenAIToolMetadata(string name, string description, JsonDocument parametersSchema)
    {
        Name = name;
        Description = description;
        ParametersSchema = parametersSchema;
    }
}
