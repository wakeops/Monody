using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Monody.AI.Tools.Capabilities.CurrentTime;
using Monody.AI.Tools.Capabilities.Weather;

namespace Monody.AI.Tools;

/// <summary>
/// Makes a malformed tool call recoverable instead of fatal.
/// </summary>
/// <remarks>
/// Two distinct problems show up here, both originating in the model's function-calling
/// arguments rather than in a plugin's own logic.
///
/// Some tools have one field that matters, and the model periodically sends its value as a bare
/// string instead of the wrapping object, e.g. {"request": "Raleigh, NC"} rather than
/// {"request": {"locationQuery": "Raleigh, NC"}}. That is coerced into the real request type
/// before invocation runs - but only when the string is not already valid JSON. Semantic Kernel
/// converts a well-formed JSON object argument into the target type on its own; unconditionally
/// wrapping every string would corrupt an already-correct payload by nesting the whole object
/// inside the one field instead (this used to happen to current_time: the model would send
/// {"request": "{\"TimeZone\":\"...\"}"} - the argument itself already a serialized object - and
/// the old version of this filter stuffed that whole string into TimeZone).
///
/// Anything else that fails to bind - a bare string for a request with more than one field, or
/// one of this project's own ArgumentException-based validation refusals (an unrecognised time
/// zone, a missing required field) - is caught around the call rather than left to propagate.
/// Propagating crashes the entire chat completion, discarding a perfectly recoverable situation:
/// every message on this path is already written to tell the model what to send instead, so
/// returning it as the tool's result lets the model actually use it and retry, rather than the
/// whole command failing with a generic "the prompt request failed".
/// </remarks>
public sealed class ToolCallRecoveryFilter : IFunctionInvocationFilter
{
    private const string RequestParameterName = "request";

    private static readonly Dictionary<string, Func<string, object>> _bareValueCoercions = new(StringComparer.Ordinal)
    {
        ["weather"] = value => new WeatherToolRequest { LocationQuery = value },
        ["current_time"] = value => new CurrentTimeToolRequest { TimeZone = value }
    };

    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        if (context.Arguments.TryGetValue(RequestParameterName, out var argument) &&
            argument is string text &&
            !IsJsonObject(text) &&
            _bareValueCoercions.TryGetValue(context.Function.Name, out var coerce))
        {
            context.Arguments[RequestParameterName] = coerce(text);
        }

        try
        {
            await next(context);
        }
        catch (ArgumentException ex)
        {
            // Covers ArgumentNullException and ArgumentOutOfRangeException too, both used
            // throughout the plugins for exactly this kind of model-facing validation message.
            context.Result = new FunctionResult(context.Function, ex.Message);
        }
    }

    private static bool IsJsonObject(string text)
    {
        try
        {
            using var document = JsonDocument.Parse(text);
            return document.RootElement.ValueKind == JsonValueKind.Object;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
