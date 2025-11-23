using Discord.WebSocket;
using Serilog.Core;
using Serilog.Events;

namespace Monody.Bot.LogEnrichers;

internal class SocketInteractionEnricher : ILogEventEnricher
{
    private readonly SocketInteraction _interaction;

    public SocketInteractionEnricher(SocketInteraction interaction)
    {
        _interaction = interaction;
    }

    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        logEvent.AddPropertiesIfAbsent(propertyFactory,
            ("InteractionId", _interaction.Id),
            ("Guild", new { Id = _interaction.GuildId }),
            DiscordLogContext.Channel(_interaction.Channel),
            DiscordLogContext.User(_interaction.User)
        );
    }
}