using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Monody.AI.SchemaJson;

/// <summary>
/// Generates a JSON Schema for OpenAI Structured Outputs (response_format).
/// Every object is emitted as a definition under "$defs" and referenced by "$ref";
/// strict mode requires "additionalProperties": false and every property listed in "required".
/// </summary>
public static class StructuredOutputSchema
{
    public static string GenerateJsonSchema<T>() => GenerateJsonSchema(typeof(T));

    public static string GenerateJsonSchema(Type rootType)
    {
        var schema = new SchemaGenerator().BuildRootSchema(rootType);

        return schema.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private sealed class SchemaGenerator
    {
        private readonly Dictionary<Type, JsonObject> _definitions = [];

        public JsonObject BuildRootSchema(Type rootType)
        {
            BuildSchema(rootType);

            if (!_definitions.TryGetValue(rootType, out var rootDefinition))
            {
                throw new InvalidOperationException($"Root type '{rootType}' is not an object type.");
            }

            // The root definition is emitted twice - inline as the top-level schema, and under
            // "$defs" so self-references stay resolvable - so the top level gets its own copy.
            var rootSchema = Clone(rootDefinition);

            var defs = new JsonObject();
            foreach (var (type, definition) in _definitions)
            {
                defs[GetDefinitionKey(type)] = definition;
            }

            rootSchema["$defs"] = defs;
            return rootSchema;
        }

        private JsonObject BuildSchema(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;

            return type.IsEnum ? BuildEnumSchema(type)
                : BuildScalarSchema(type)
                ?? BuildArraySchema(type)
                ?? BuildDictionarySchema(type)
                ?? BuildObjectReference(type);
        }

        /// <summary>Registers <paramref name="type"/> under "$defs" and returns a "$ref" to it.</summary>
        private JsonObject BuildObjectReference(Type type)
        {
            var reference = new JsonObject { ["$ref"] = $"#/$defs/{GetDefinitionKey(type)}" };

            if (_definitions.ContainsKey(type))
            {
                return reference;
            }

            // Registered before recursing into properties so self-referencing types terminate.
            var definition = new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = false
            };
            _definitions[type] = definition;

            var properties = new JsonObject();

            foreach (var property in GetEligibleProperties(type))
            {
                var propertySchema = BuildSchema(property.PropertyType);

                // Structured Outputs forbids siblings of "$ref", so descriptions and
                // constraints are only applied to inline schemas.
                if (!propertySchema.ContainsKey("$ref"))
                {
                    var description = property.GetCustomAttribute<DescriptionAttribute>()?.Description;
                    if (!string.IsNullOrWhiteSpace(description))
                    {
                        propertySchema["description"] = description;
                    }

                    ApplyValidationAttributes(property, propertySchema);
                }

                properties[GetJsonPropertyName(property)] = propertySchema;
            }

            definition["properties"] = properties;

            // Strict mode requires every property to be required; optionality is expressed
            // by the property's own type instead.
            definition["required"] = new JsonArray([.. properties.Select(p => (JsonNode)p.Key)]);

            return reference;
        }

        private JsonObject BuildArraySchema(Type type)
        {
            var elementType = type.IsArray
                ? type.GetElementType()
                : FindGenericInterface(type, typeof(IEnumerable<>))?.GetGenericArguments()[0];

            if (elementType is null || elementType == typeof(char))
            {
                return null;
            }

            return new JsonObject
            {
                ["type"] = "array",
                ["items"] = BuildSchema(elementType)
            };
        }

        private JsonObject BuildDictionarySchema(Type type)
        {
            var dictionary = FindGenericInterface(type, typeof(IDictionary<,>))
                             ?? FindGenericInterface(type, typeof(IReadOnlyDictionary<,>));

            // JSON object keys are always strings, so anything else can't be modelled as an object.
            if (dictionary?.GetGenericArguments() is not [var keyType, var valueType] || keyType != typeof(string))
            {
                return null;
            }

            return new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = BuildSchema(valueType)
            };
        }

        private static JsonObject BuildEnumSchema(Type enumType)
        {
            // String enums read better to the model than the underlying numeric values.
            var schema = new JsonObject
            {
                ["type"] = "string",
                ["enum"] = new JsonArray([.. Enum.GetNames(enumType).Select(n => (JsonNode)n)])
            };

            var description = enumType.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (!string.IsNullOrWhiteSpace(description))
            {
                schema["description"] = description;
            }

            return schema;
        }

        private static JsonObject BuildScalarSchema(Type type) => type switch
        {
            _ when type == typeof(string) => new() { ["type"] = "string" },
            _ when type == typeof(bool) => new() { ["type"] = "boolean" },
            _ when type == typeof(byte) || type == typeof(sbyte)
                || type == typeof(short) || type == typeof(ushort)
                || type == typeof(int) || type == typeof(uint)
                || type == typeof(long) || type == typeof(ulong) => new() { ["type"] = "integer" },
            _ when type == typeof(float) || type == typeof(double) || type == typeof(decimal) => new() { ["type"] = "number" },
            _ when type == typeof(DateTime) || type == typeof(DateTimeOffset) => Described("ISO 8601 timestamp string."),
            _ when type == typeof(Guid) => Described("GUID string."),
            _ when type == typeof(Uri) => Described("URL string."),
            _ => null
        };

        private static JsonObject Described(string description) =>
            new() { ["type"] = "string", ["description"] = description };

        private static void ApplyValidationAttributes(PropertyInfo property, JsonObject schema)
        {
            var isString = schema["type"]?.ToString() == "string";
            var isArray = schema["type"]?.ToString() == "array";

            if (property.GetCustomAttribute<RangeAttribute>() is { } range)
            {
                if (ToDouble(range.Minimum) is { } minimum)
                {
                    schema["minimum"] = minimum;
                }

                if (ToDouble(range.Maximum) is { } maximum)
                {
                    schema["maximum"] = maximum;
                }
            }

            if (property.GetCustomAttribute<StringLengthAttribute>() is { } stringLength)
            {
                if (stringLength.MinimumLength > 0)
                {
                    schema["minLength"] = stringLength.MinimumLength;
                }

                if (stringLength.MaximumLength > 0)
                {
                    schema["maxLength"] = stringLength.MaximumLength;
                }
            }

            if (property.GetCustomAttribute<MaxLengthAttribute>() is { } maxLength)
            {
                if (isString)
                {
                    schema["maxLength"] = maxLength.Length;
                }
                else if (isArray)
                {
                    schema["maxItems"] = maxLength.Length;
                }
            }

            if (property.GetCustomAttribute<MinLengthAttribute>() is { } minLength)
            {
                if (isString)
                {
                    schema["minLength"] = minLength.Length;
                }
                else if (isArray)
                {
                    schema["minItems"] = minLength.Length;
                }
            }
        }

        private static IEnumerable<PropertyInfo> GetEligibleProperties(Type type) =>
            type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetMethod is { IsPublic: true, IsStatic: false })
                .Where(p => p.GetIndexParameters().Length == 0)
                .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() is null);

        private static string GetJsonPropertyName(PropertyInfo property) =>
            property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name ?? property.Name;

        // Nested types use '+' in their full name, which isn't valid in a "$ref" pointer.
        private static string GetDefinitionKey(Type type) => (type.FullName ?? type.Name).Replace('+', '.');

        private static Type FindGenericInterface(Type type, Type genericInterface)
        {
            if (type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == genericInterface)
            {
                return type;
            }

            return type.GetInterfaces()
                       .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterface);
        }

        private static double? ToDouble(object value) => value switch
        {
            byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal
                => Convert.ToDouble(value, CultureInfo.InvariantCulture),
            string text when double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                => parsed,
            _ => null
        };

        private static JsonObject Clone(JsonObject value) => (JsonObject)JsonNode.Parse(value.ToJsonString());
    }
}
