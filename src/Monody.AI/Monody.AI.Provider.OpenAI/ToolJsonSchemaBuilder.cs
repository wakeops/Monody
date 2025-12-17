using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Monody.AI.Tools.ToolHandler;

namespace Monody.AI.Provider.OpenAI;

public static class ToolJsonSchemaBuilder
{
    public static JsonDocument FromParameters(List<ToolParameterSchema> parameters)
    {
        var schema = new JsonObject
        {
            ["type"] = "object"
        };

        var propertiesObj = new JsonObject();
        var requiredList = new Dictionary<int, JsonArray>();

        foreach (var param in parameters)
        {
            var jsonName = param.Name;

            var propSchema = new JsonObject
            {
                ["type"] = param.JsonType
            };

            propSchema["description"] ??= param.Description;
            propSchema["default"] ??= GetDefaultValue(param.DefaultValue);
            propSchema["minimum"] ??= param.MinValue;
            propSchema["maximum"] ??= param.MaxValue;

            if (param.RequiredGroupId.HasValue)
            {
                if (!requiredList.ContainsKey(param.RequiredGroupId.Value))
                {
                    requiredList[param.RequiredGroupId.Value] = [];
                }

                requiredList[param.RequiredGroupId.Value].Add(jsonName);
            }

            if (param.Type.IsEnum)
            {
                var enumValues = new JsonArray();
                foreach (var enumName in Enum.GetNames(param.Type))
                {
                    enumValues.Add(enumName);
                }
                propSchema["enum"] = enumValues;
            }

            propertiesObj[jsonName] = propSchema;
        }

        schema["properties"] = propertiesObj;

        if (requiredList.Count == 1)
        {
            schema["required"] = requiredList.First().Value;
        }
        else if (requiredList.Count > 1)
        {
            var oneOfArray = new JsonArray();
            foreach (var reqGroup in requiredList.Values)
            {
                var reqObj = new JsonObject
                {
                    ["required"] = reqGroup
                };
                oneOfArray.Add(reqObj);
            }
            schema["oneOf"] = oneOfArray;
        }

        return JsonDocument.Parse(schema.ToJsonString());
    }

    private static string GetDefaultValue(object value)
    {
        if (value == null)
        {
            return null;
        }

        var valueType = value.GetType();

        if (valueType.IsEnum)
        {
            return Enum.GetName(valueType, value) ?? string.Empty;
        }

        return JsonSerializer.Serialize(value);
    }
}
