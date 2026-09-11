using Discord;
using Discord.WebSocket;
using Monody.AI.Tools.Abstractions;

namespace Monody.AI.Tools;

internal static class InvocationContextExtensions
{
    public static void EnsureIsGuildInstall(this IInvocationContext context)
    {
        if (context.Interaction?.IntegrationOwners.ContainsKey(ApplicationIntegrationType.GuildInstall) != true)
        {
            throw new InvalidOperationException("This tool is only available when the application is installed to the server, not when it is only running as a personal app.");
        }
    }

    public static void EnsureCanReadMessages(this IInvocationContext context)
    {
        var channel = context.Interaction?.Channel;

        if (channel is not SocketGuildChannel guildChannel)
        {
            return;
        }

        var permissions = guildChannel.Guild.CurrentUser.GetPermissions(guildChannel);

        if (!permissions.ViewChannel || !permissions.ReadMessageHistory)
        {
            throw new InvalidOperationException($"The application does not have permission to read channel '{channel.Id}'.");
        }
    }

    public static ulong RequireUserId(this IInvocationContext context)
    {
        if (context.Interaction?.User is null)
        {
            throw new InvalidOperationException("No Discord user is in scope, so the tool cannot be used.");
        }

        return context.Interaction.User.Id;
    }
}
