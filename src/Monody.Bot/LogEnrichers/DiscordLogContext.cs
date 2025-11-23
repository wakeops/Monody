using Discord;

namespace Monody.Bot.LogEnrichers;

internal static class DiscordLogContext
{
    public static (string, object) User(IUser user) =>
       ("User", new { user.Id, user.Username, user.IsBot });

    public static (string, object) Channel(IChannel channel) =>
        ("Channel", new { channel.Id, channel.Name, Type = channel.GetChannelType() });
}
