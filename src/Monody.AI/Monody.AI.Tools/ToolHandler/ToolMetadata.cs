using System.Text.Json;

namespace Monody.AI.Tools.ToolHandler;

public sealed class ToolMetadata
{
    public string Name { get; }

    public string Description { get; }

    public JsonDocument ParametersSchema { get; }

    public ToolMetadata(string name, string description, JsonDocument parametersSchema)
    {
        Name = name;
        Description = description;
        ParametersSchema = parametersSchema;
    }
}
