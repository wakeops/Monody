using System;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace Monody.AI.Tools.Capabilities.Weather;

/// <summary>
/// The model occasionally calls the "weather" tool with a bare location string instead of the
/// expected request object, e.g. {"request": "Raleigh, NC"} instead of
/// {"request": {"locationQuery": "Raleigh, NC"}}. Semantic Kernel passes that string straight through
/// to the reflection-invoked method without converting it, which throws an ArgumentException.
/// Coerce it into a proper WeatherToolRequest before invocation runs.
/// </summary>
public sealed class WeatherRequestCoercionFilter : IFunctionInvocationFilter
{
    public Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        if (context.Function.Name == "weather" &&
            context.Arguments.TryGetValue("request", out var value) &&
            value is string locationQuery)
        {
            context.Arguments["request"] = new WeatherToolRequest { LocationQuery = locationQuery };
        }

        return next(context);
    }
}
