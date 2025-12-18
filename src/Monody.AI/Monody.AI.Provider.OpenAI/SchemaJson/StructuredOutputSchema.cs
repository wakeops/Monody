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

namespace Monody.AI.Provider.OpenAI.SchemaJson;

public static class StructuredOutputSchema
{
    /// <summary>
    /// Generates a JSON Schema suitable for OpenAI Structured Outputs response_format.
    /// Top-level schema is always { "type": "object" }.
    /// </summary>
    public static string GenerateJsonSchema<T>(
        bool additionalProperties = false,
        bool inferRequiredFromNullability = false)
        => GenerateJsonSchema(typeof(T), additionalProperties, inferRequiredFromNullability);

    public static string GenerateJsonSchema(
        Type rootType,
        bool additionalProperties = false,
        bool inferRequiredFromNullability = false)
    {
        var ctx = new GeneratorContext(additionalProperties, inferRequiredFromNullability);

        var schema = ctx.BuildRootSchema(rootType);

        // If the root type isn't an object, wrap it in an object so the top-level stays valid for constraints.
        if (schema?["type"]?.ToString() != "object")
        {
            var wrapper = new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = ctx.AdditionalProperties,
                ["properties"] = new JsonObject
                {
                    ["value"] = schema
                },
                ["required"] = new JsonArray("value")
            };
            schema = wrapper;
        }

        return schema.ToJsonString(new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }

    private sealed class GeneratorContext
    {
        private readonly Dictionary<Type, JsonObject> _definitions = [];
        private readonly NullabilityInfoContext _nullabilityContext = new();

        public bool AdditionalProperties { get; }
        public bool InferRequiredFromNullability { get; }

        public GeneratorContext(bool additionalProperties, bool inferRequiredFromNullability)
        {
            AdditionalProperties = additionalProperties;
            InferRequiredFromNullability = inferRequiredFromNullability;
        }

        public JsonObject BuildObjectSchema(Type type)
        {
            var schema = BuildSchema(type, declaringProperty: null);

            if (_definitions.Count > 0)
            {
                var defs = new JsonObject();
                foreach (var kvp in _definitions)
                {
                    defs[GetDefinitionKey(kvp.Key)] = kvp.Value;
                }

                schema["$defs"] = defs;
            }

            return schema;
        }

        public JsonObject BuildSchema(Type type, PropertyInfo declaringProperty = null)
        {
            type = UnwrapNullable(type, out _);

            // Special-case: enums
            if (type.IsEnum)
            {
                return BuildEnumSchema(type);
            }

            // Primitives / scalar types
            if (TryBuildScalarSchema(type, out var scalar))
            {
                // Nullable<T> value types can be represented by omitting required; JSON Schema "null" union is optional.
                return scalar;
            }

            // Arrays / lists
            if (TryBuildArraySchema(type, declaringProperty, out var arraySchema))
            {
                return arraySchema;
            }

            // Dictionaries (string-keyed) -> object with additionalProperties = value schema
            if (TryBuildDictionarySchema(type, out var dictSchema))
            {
                return dictSchema;
            }

            // Complex object
            return BuildComplexObjectSchema(type);
        }

        public JsonObject BuildRootSchema(Type rootType)
        {
            // Ensure root definition exists in _definitions (as the canonical instance)
            var rootDefKey = GetDefinitionKey(rootType);

            // Build schema for rootType, but force it into _definitions
            BuildSchema(rootType, declaringProperty: null);

            if (!_definitions.TryGetValue(rootType, out var rootDef))
                throw new InvalidOperationException($"Root definition '{rootDefKey}' was not generated.");

            // Root schema should be an object; use a clone so it can have its own parent.
            var rootSchema = DeepClone(rootDef);

            // Attach $defs (also clones to avoid parent conflicts)
            var defs = new JsonObject();
            foreach (var kvp in _definitions)
            {
                defs[GetDefinitionKey(kvp.Key)] = DeepClone(kvp.Value);
            }

            rootSchema["$defs"] = defs;
            return rootSchema;
        }

        private static JsonObject DeepClone(JsonObject obj)
        {
            // Round-trip clone; safe and simple for schema objects.
            return (JsonObject)JsonNode.Parse(obj.ToJsonString())!;
        }

        private JsonObject BuildComplexObjectSchema(Type type)
        {
            if (_definitions.TryGetValue(type, out _))
            {
                return new JsonObject { ["$ref"] = $"#/$defs/{GetDefinitionKey(type)}" };
            }

            var def = new JsonObject
            {
                ["type"] = "object",
                ["additionalProperties"] = AdditionalProperties
            };
            _definitions[type] = def;

            var props = new JsonObject();
            var required = new JsonArray();

            foreach (var prop in GetEligibleProperties(type))
            {
                var jsonName = GetJsonPropertyName(prop);
                var propSchema = BuildSchema(prop.PropertyType, prop);

                var description = prop.GetCustomAttribute<DescriptionAttribute>()?.Description
                                  ?? prop.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description;

                if (!string.IsNullOrWhiteSpace(description))
                {
                    // OpenAI Structured Outputs: $ref cannot have sibling keywords like description
                    if (propSchema is JsonObject o && o.ContainsKey("$ref"))
                    {
                        // Optionally: push the description onto the referenced definition instead
                        // but simplest is: do nothing here.
                    }
                    else
                    {
                        propSchema["description"] = description;
                    }
                }

                ApplyValidationAttributes(prop, propSchema);

                props[jsonName] = propSchema;

                if (IsRequired(prop))
                {
                    required.Add(jsonName);
                }
            }

            def["properties"] = props;

            def["required"] = new JsonArray(props.Select(kvp => (JsonNode)kvp.Key).ToArray());

            return new JsonObject { ["$ref"] = $"#/$defs/{GetDefinitionKey(type)}" };
        }

        private static IEnumerable<PropertyInfo> GetEligibleProperties(Type type) =>
            type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetMethod is { IsPublic: true } && !p.GetMethod.IsStatic)
                .Where(p => p.GetIndexParameters().Length == 0)
                .Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() is null);

        private bool IsRequired(PropertyInfo prop)
        {
            if (prop.GetCustomAttribute<RequiredAttribute>() is not null)
            {
                return true;
            }

            if (!InferRequiredFromNullability)
            {
                return false;
            }

            // Value types: required unless Nullable<T>
            if (prop.PropertyType.IsValueType)
            {
                return Nullable.GetUnderlyingType(prop.PropertyType) is null;
            }

            // Reference types: required if declared non-nullable
            var nullInfo = _nullabilityContext.Create(prop);
            return nullInfo.ReadState == NullabilityState.NotNull;
        }

        private static Type UnwrapNullable(Type type, out bool wasNullableValueType)
        {
            var underlying = Nullable.GetUnderlyingType(type);
            if (underlying is not null)
            {
                wasNullableValueType = true;
                return underlying;
            }

            wasNullableValueType = false;
            return type;
        }

        private static string GetDefinitionKey(Type t)
        {
            // Stable-ish key; you can replace with your own naming policy.
            // Avoid '+' for nested types.
            return (t.FullName ?? t.Name).Replace('+', '.');
        }

        private static string GetJsonPropertyName(PropertyInfo prop) =>
            prop.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                //?? options.PropertyNamingPolicy?.ConvertName(prop.Name)
                ?? prop.Name;

        private static JsonObject BuildEnumSchema(Type enumType)
        {
            // Use string enums by default (more model-friendly).
            // If you want numeric enums, switch "type" to "integer" and enum values accordingly.
            var names = Enum.GetNames(enumType);

            var schema = new JsonObject
            {
                ["type"] = "string",
                ["enum"] = new JsonArray(names.Select(n => (JsonNode)n).ToArray())
            };

            // Description from [Description] on enum type (rare but possible)
            var enumDesc = enumType.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (!string.IsNullOrWhiteSpace(enumDesc))
            {
                schema["description"] = enumDesc;
            }

            return schema;
        }

        private static bool TryBuildScalarSchema(Type type, out JsonObject schema)
        {
            schema = [];

            // Strings
            if (type == typeof(string))
            {
                schema["type"] = "string";
                return true;
            }

            // Booleans
            if (type == typeof(bool))
            {
                schema["type"] = "boolean";
                return true;
            }

            // Integers
            if (type == typeof(byte) || type == typeof(sbyte) ||
                type == typeof(short) || type == typeof(ushort) ||
                type == typeof(int) || type == typeof(uint) ||
                type == typeof(long) || type == typeof(ulong))
            {
                schema["type"] = "integer";
                return true;
            }

            // Numbers
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            {
                schema["type"] = "number";
                return true;
            }

            // Date/time-like: represent as string
            if (type == typeof(DateTime) || type == typeof(DateTimeOffset))
            {
                schema["type"] = "string";
                schema["description"] = "ISO 8601 timestamp string.";
                return true;
            }

            if (type == typeof(Guid))
            {
                schema["type"] = "string";
                schema["description"] = "GUID string.";
                return true;
            }

            if (type == typeof(Uri))
            {
                schema["type"] = "string";
                schema["description"] = "URL string.";
                return true;
            }

            return false;
        }

        private bool TryBuildArraySchema(Type type, PropertyInfo prop, out JsonObject schema)
        {
            schema = [];

            Type elementType = null;

            if (type.IsArray)
            {
                elementType = type.GetElementType();
            }
            else
            {
                // Handle IEnumerable<T>, List<T>, etc.
                var ienum = FindGenericInterface(type, typeof(IEnumerable<>));
                if (ienum is not null)
                {
                    elementType = ienum.GetGenericArguments()[0];
                }
            }

            if (elementType is null || elementType == typeof(char))
            {
                return false;
            }

            schema["type"] = "array";
            schema["items"] = BuildSchema(elementType);

            // Apply length constraints if present
            ApplyCollectionLengthAttributes(prop, schema);

            return true;
        }

        private bool TryBuildDictionarySchema(Type type, out JsonObject schema)
        {
            schema = [];

            var idict = FindGenericInterface(type, typeof(IDictionary<,>))
                        ?? FindGenericInterface(type, typeof(IReadOnlyDictionary<,>));

            if (idict is null)
            {
                return false;
            }

            var args = idict.GetGenericArguments();
            var keyType = args[0];
            var valueType = args[1];

            // JSON object keys must be strings in practice.
            if (keyType != typeof(string))
            {
                return false;
            }

            schema["type"] = "object";
            schema["additionalProperties"] = BuildSchema(valueType);

            return true;
        }

        private static Type FindGenericInterface(Type type, Type genericInterface)
        {
            if (type.IsInterface && type.IsGenericType && type.GetGenericTypeDefinition() == genericInterface)
            {
                return type;
            }

            return type.GetInterfaces()
                       .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == genericInterface);
        }

        private static void ApplyValidationAttributes(PropertyInfo prop, JsonObject propSchema)
        {
            // Range
            if (prop.GetCustomAttribute<RangeAttribute>() is RangeAttribute range)
            {
                if (TryConvertToDouble(range.Minimum, out var min))
                {
                    propSchema["minimum"] = min;
                }

                if (TryConvertToDouble(range.Maximum, out var max))
                {
                    propSchema["maximum"] = max;
                }
            }

            // StringLength
            if (prop.GetCustomAttribute<StringLengthAttribute>() is StringLengthAttribute sl)
            {
                if (sl.MinimumLength > 0)
                {
                    propSchema["minLength"] = sl.MinimumLength;
                }

                if (sl.MaximumLength > 0)
                {
                    propSchema["maxLength"] = sl.MaximumLength;
                }
            }

            // MaxLength / MinLength (strings + arrays)
            if (prop.GetCustomAttribute<MaxLengthAttribute>() is MaxLengthAttribute mx)
            {
                if (propSchema["type"]?.ToString() == "string")
                {
                    propSchema["maxLength"] = mx.Length;
                }
                else if (propSchema["type"]?.ToString() == "array")
                {
                    propSchema["maxItems"] = mx.Length;
                }
            }

            if (prop.GetCustomAttribute<MinLengthAttribute>() is MinLengthAttribute mn)
            {
                if (propSchema["type"]?.ToString() == "string")
                {
                    propSchema["minLength"] = mn.Length;
                }
                else if (propSchema["type"]?.ToString() == "array")
                {
                    propSchema["minItems"] = mn.Length;
                }
            }
        }

        private static void ApplyCollectionLengthAttributes(PropertyInfo prop, JsonObject arraySchema)
        {
            if (prop is null)
            {
                return;
            }

            if (prop.GetCustomAttribute<MaxLengthAttribute>() is MaxLengthAttribute mx)
            {
                arraySchema["maxItems"] = mx.Length;
            }

            if (prop.GetCustomAttribute<MinLengthAttribute>() is MinLengthAttribute mn)
            {
                arraySchema["minItems"] = mn.Length;
            }
        }

        private static bool TryConvertToDouble(object value, out double result)
        {
            switch (value)
            {
                case byte b: result = b; return true;
                case sbyte sb: result = sb; return true;
                case short s: result = s; return true;
                case ushort us: result = us; return true;
                case int i: result = i; return true;
                case uint ui: result = ui; return true;
                case long l: result = l; return true;
                case ulong ul: result = ul; return true;
                case float f: result = f; return true;
                case double d: result = d; return true;
                case decimal m: result = (double)m; return true;
                case string str when double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed):
                    result = parsed; return true;
                default:
                    result = 0;
                    return false;
            }
        }
    }
}
