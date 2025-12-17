using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using Monody.AI.Tools.ToolHandler;

namespace Monody.AI.Provider.OpenAI;

internal static class ToolJsonSchemaBuilder
{
    public static JsonDocument FromParameters(List<ToolParameterSchema> parameters)
    {
        var schema = new JsonObject
        {
            ["type"] = "object"
        };

        var propertiesObj = new JsonObject();
        var requiredList = new JsonArray();

        foreach (var param in parameters)
        {
            var jsonName = param.Name;

            var propSchema = new JsonObject
            {
                ["type"] = param.JsonType
            };

            propSchema["description"] ??= param.Description;

            if (param.IsRequired)
            {
                requiredList.Add(jsonName);
            }

            propertiesObj[jsonName] = propSchema;
        }

        schema["properties"] = propertiesObj;

        if (requiredList.Count > 0)
        {
            schema["required"] = requiredList;
        }

        return JsonDocument.Parse(schema.ToJsonString());
    }
}
