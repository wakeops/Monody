using Microsoft.SemanticKernel.ChatCompletion;

namespace Monody.Bot.Modules.Slop.Models;

public sealed record DiscordConversation(
    ulong Id,
    ulong? GuildId,
    ulong? ChannelId,
    ulong InitialUserId,
    ChatHistory History);
