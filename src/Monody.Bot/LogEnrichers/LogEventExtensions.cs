using Serilog.Core;
using Serilog.Events;

namespace Monody.Bot.LogEnrichers;

internal static class LogEventExtensions
{
    public static void AddPropertiesIfAbsent(this LogEvent logEvent, ILogEventPropertyFactory propertyFactory, params (string Key, object Value)[] props)
    {
        foreach (var (key, value) in props)
        {
            logEvent.AddPropertyIfAbsent(propertyFactory, key, value);
        }
    }
}
