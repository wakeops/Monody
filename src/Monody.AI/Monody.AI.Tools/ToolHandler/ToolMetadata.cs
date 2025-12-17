using System.Collections.Generic;

namespace Monody.AI.Tools.ToolHandler;

public sealed class ToolMetadata
{
    public string Name { get; }

    public string Description { get; }

    public List<ToolParameterSchema> Parameters { get; }

    public ToolMetadata(string name, string description, List<ToolParameterSchema> parameters)
    {
        Name = name;
        Description = description;
        Parameters = parameters;
    }
}
