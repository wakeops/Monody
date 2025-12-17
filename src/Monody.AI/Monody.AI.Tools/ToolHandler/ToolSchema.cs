using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Monody.AI.Tools.ToolHandler;

public sealed class ToolParameterSchema
{
    public string Name { get; init; }

    public Type Type { get; init; }

    public string JsonType { get; init; }

    public string Description { get; init; }

    public bool IsRequired { get; init; }
}

public static class ToolSchema
{
    public static List<ToolParameterSchema> ParametersFromType<T>()
    {
        var schemaType = typeof(T);

        var parameters = new List<ToolParameterSchema>();

        foreach (var prop in schemaType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            // Determine JSON property name
            var jsonName = prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                           ?? prop.Name;

            // Description attribute
            var descAttr = prop.GetCustomAttribute<DescriptionAttribute>();
            var description = descAttr?.Description ?? string.Empty;

            // Required attribute
            var isRequired = prop.GetCustomAttribute<RequiredAttribute>() != null;

            parameters.Add(new ToolParameterSchema
            {
                Name = jsonName,
                Type = prop.PropertyType,
                JsonType = MapDotNetTypeToJsonType(prop.PropertyType),
                Description = description,
                IsRequired = isRequired
            });
        }

        return parameters;
    }

    private static string MapDotNetTypeToJsonType(Type t)
    {
        ArgumentNullException.ThrowIfNull(t);

        // Unwrap Nullable<T>
        t = Nullable.GetUnderlyingType(t) ?? t;

        // Boolean
        if (t == typeof(bool))
        {
            return "boolean";
        }

        if (IsNumeric(t))
        {
            return "number";
        }

        if (t == typeof(string) ||
            t == typeof(char) ||
            t == typeof(Guid) ||
            t == typeof(DateTime) ||
            t == typeof(DateTimeOffset) ||
            t == typeof(TimeSpan))
        {
            return "string";
        }

        if (t.IsArray || (t.IsGenericType && typeof(System.Collections.IEnumerable).IsAssignableFrom(t)))
        {
            return "array";
        }

        return "object";
    }

    private static bool IsNumeric(Type t)
    {
        return t == typeof(byte) ||
               t == typeof(sbyte) ||
               t == typeof(short) ||
               t == typeof(ushort) ||
               t == typeof(int) ||
               t == typeof(uint) ||
               t == typeof(long) ||
               t == typeof(ulong) ||
               t == typeof(float) ||
               t == typeof(double) ||
               t == typeof(decimal) ||
               t == typeof(BigInteger);
    }
}
