using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Monody.Module.AIChat.Tools;

internal static class JsonSchemaGenerator
{
    public static string GenerateSchemaFor<TSchemaType>()
        where TSchemaType : class
    {
        var schema = new JsonObject
        {
            ["type"] = "object"
        };

        var propertiesObj = new JsonObject();
        var requiredList = new JsonArray();

        foreach (var prop in typeof(TSchemaType).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Determine JSON property name
            var jsonName = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                           ?? prop.Name;

            var propSchema = new JsonObject
            {
                ["type"] = MapDotNetTypeToJsonType(prop.PropertyType)
            };

            // Description attribute
            var descAttr = prop.GetCustomAttribute<DescriptionAttribute>();
            if (descAttr != null)
            {
                propSchema["description"] = descAttr.Description;
            }

            // Required attribute → add to required array
            var requiredAttr = prop.GetCustomAttribute<RequiredAttribute>();
            if (requiredAttr != null)
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

        return schema.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private static string MapDotNetTypeToJsonType(Type t)
    {
        if (t == typeof(string))
        {
            return "string";
        }

        if (t == typeof(int) || t == typeof(long) || t == typeof(short) ||
            t == typeof(float) || t == typeof(double) || t == typeof(decimal))
        {
            return "number";
        }

        if (t == typeof(bool))
        {
            return "boolean";
        }

        if (t.IsArray || (t.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(t)))
        {
            return "array";
        }

        return "object";
    }
}