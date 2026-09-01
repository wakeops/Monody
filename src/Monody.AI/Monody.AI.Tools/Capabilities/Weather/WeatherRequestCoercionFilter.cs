using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Monody.AI.Tools.Capabilities.Weather;

/// <summary>
/// The model occasionally calls the "weather" tool with a bare location string instead of the
/// expected request object, e.g. {"request": "Raleigh, NC"} instead of
/// {"request": {"locationQuery": "Raleigh, NC"}}. Semantic Kernel only auto-converts JsonElement/
/// JsonDocument/JsonNode argument values; a plain CLR string is passed straight through to the
/// reflection-invoked method unconverted, which throws an ArgumentException. Coerce any string
/// argument on this function into a proper WeatherToolRequest before invocation runs, and log
/// what we saw if invocation still fails so a mismatch we didn't anticipate is visible.
/// </summary>
public sealed class WeatherRequestCoercionFilter(ILogger<WeatherRequestCoercionFilter> logger) : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(FunctionInvocationContext context, Func<FunctionInvocationContext, Task> next)
    {
        if (context.Function.Name != "weather")
        {
            await next(context);
            return;
        }

        foreach (var name in context.Arguments.Names.ToList())
        {
            if (context.Arguments[name] is string locationQuery)
            {
                context.Arguments[name] = new WeatherToolRequest { LocationQuery = locationQuery };
            }
        }

        try
        {
            await next(context);
        }
        catch (ArgumentException ex) when (ex.Message.Contains(nameof(WeatherToolRequest)))
        {
            var argSummary = string.Join(", ", context.Arguments.Names.Select(n => $"{n}={context.Arguments[n]} [{context.Arguments[n]?.GetType().FullName ?? "null"}]"));
            logger.LogError(ex, "Weather tool invocation failed after coercion attempt. Arguments: {Arguments}", argSummary);
            throw;
        }
    }
}
