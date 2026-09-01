using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Monody.AI.Tools.Capabilities.CurrentTime;
using Monody.AI.Tools.Capabilities.Weather;

namespace Monody.AI.Tools;

/// <summary>
/// Tools whose request object has one obvious field get called with a bare string instead, e.g.
/// {"request": "Raleigh, NC"} rather than {"request": {"locationQuery": "Raleigh, NC"}}. Semantic
/// Kernel hands that string straight to the reflection-invoked method without converting it, which
/// throws. Coerce it into the real request type before invocation runs.
/// </summary>
public sealed class BareStringRequestCoercionFilter : IFunctionInvocationFilter
{
    private static readonly Dictionary<string, Func<string, object>> _coercions = new(StringComparer.Ordinal)
    {
        ["weather"] = value => new WeatherToolRequest { LocationQuery = value },
        ["current_time"] = value => new CurrentTimeToolRequest { TimeZone = value }
    };

    public Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        if (context.Arguments.TryGetValue("request", out var argument) &&
            argument is string bareValue &&
            _coercions.TryGetValue(context.Function.Name, out var coerce))
        {
            context.Arguments["request"] = coerce(bareValue);
        }

        return next(context);
    }
}
