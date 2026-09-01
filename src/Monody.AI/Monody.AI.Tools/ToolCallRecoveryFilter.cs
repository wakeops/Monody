using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Monody.AI.Tools.Capabilities.CurrentTime;
using Monody.AI.Tools.Capabilities.Weather;

namespace Monody.AI.Tools;

/// <summary>
/// Makes a malformed tool call recoverable instead of fatal.
/// </summary>
/// <remarks>
/// Three distinct problems show up here, all originating in the model's function-calling
/// arguments rather than in a plugin's own logic.
///
/// Some tools have one field that matters, and the model periodically sends its value as a bare
/// string instead of the wrapping object, e.g. {"request": "Raleigh, NC"} rather than
/// {"request": {"locationQuery": "Raleigh, NC"}}. That is coerced into the real request type
/// before invocation runs.
///
/// When the argument IS a well-formed JSON object, Semantic Kernel's own fallback conversion from
/// string to the target type is not reliable for anything beyond the simplest shapes - it silently
/// fails (throwing System.ArgumentException: "Object of type 'System.String' cannot be converted
/// to type '...'") for request types with an enum property, such as remember's RememberToolRequest
/// {Category, Content}. Left uncaught this used to surface as the model reporting success while
/// nothing was actually saved, because the plugin method never ran at all - the binding failure
/// happened first, and (before this filter existed) had nowhere to go but the model's own retry
/// logic, which apparently sometimes just hallucinated success instead. So instead of trusting
/// Semantic Kernel's fallback, this filter deserializes the argument itself, using the function's
/// own parameter metadata to get the real target type and a JsonStringEnumConverter so enum
/// properties bind correctly regardless of casing. Only when that self-deserialization fails (the
/// string isn't valid JSON at all - a bare value, or one of this project's own ArgumentException
/// refusals) does it fall back to the bare-value coercion map above.
///
/// Anything else that fails to bind - a bare string for a request with more than one field and no
/// safe single-value guess, one of this project's own ArgumentException-based validation refusals
/// (an unrecognised time zone, a missing required field), or the model omitting the "request"
/// argument entirely for a tool whose request has nothing required (recall's did: {"Unused": "..."}
/// with no [Required] on it, so a well-formed call can legally supply no arguments at all -
/// Semantic Kernel still throws KernelException("Missing argument for function parameter
/// 'request'") wrapping an ArgumentException, since the parameter itself is non-optional even when
/// every one of its properties is) - is caught around the call rather than left to propagate.
/// Propagating crashes the entire chat completion, discarding a perfectly recoverable situation:
/// every message on this path is already written to tell the model what to send instead, so
/// returning it as the tool's result lets the model actually use it and retry, rather than the
/// whole command failing with a generic "the prompt request failed".
/// </remarks>
public sealed class ToolCallRecoveryFilter : IFunctionInvocationFilter
{
    private const string RequestParameterName = "request";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static readonly Dictionary<string, Func<string, object>> _bareValueCoercions = new(StringComparer.Ordinal)
    {
        ["weather"] = value => new WeatherToolRequest { LocationQuery = value },
        ["current_time"] = value => new CurrentTimeToolRequest { TimeZone = value }
    };

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        if (context.Arguments.TryGetValue(RequestParameterName, out var argument) && argument is string text)
        {
            var parameterType = context.Function.Metadata.Parameters
                .FirstOrDefault(p => p.Name == RequestParameterName)?.ParameterType;

            if (parameterType is not null && parameterType != typeof(string))
            {
                if (TryDeserialize(text, parameterType, out var deserialized))
                {
                    context.Arguments[RequestParameterName] = deserialized;
                }
                else if (_bareValueCoercions.TryGetValue(context.Function.Name, out var coerce))
                {
                    context.Arguments[RequestParameterName] = coerce(text);
                }
            }
        }

        try
        {
            await next(context);
        }
        catch (Exception ex) when (ex is ArgumentException || ex.InnerException is ArgumentException)
        {
            // Covers ArgumentNullException and ArgumentOutOfRangeException directly (both used
            // throughout the plugins for this kind of model-facing validation message), and
            // Semantic Kernel's own KernelException, which wraps an ArgumentException rather
            // than being one, for argument-binding failures such as a missing required parameter.
            context.Result = new FunctionResult(context.Function, ex.Message);
        }
    }

    private static bool TryDeserialize(string text, Type type, out object value)
    {
        try
        {
            var result = JsonSerializer.Deserialize(text, type, _jsonOptions);
            value = result;
            return result is not null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
