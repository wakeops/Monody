using System.Text.Json;
using System.Text.Json.Serialization;
using Discord;

namespace Monody.Bot.Modules.Slop.Models;

/// <summary>The user's prompt plus who sent it, serialized as JSON for the model to read.</summary>
internal sealed class DiscordUserPrompt
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public DiscordPromptContext DiscordContext { get; }

    public string Prompt { get; }

    public DiscordUserPrompt(IUser user, string prompt)
    {
        Prompt = prompt.Trim();
        DiscordContext = new DiscordPromptContext(new DiscordPromptUser(user.Id, user.Username));
    }

    public override string ToString() => JsonSerializer.Serialize(this, _jsonOptions);
}

internal sealed record DiscordPromptContext(DiscordPromptUser User);

internal sealed record DiscordPromptUser(ulong Id, string Username);
