using System.Text.Json;
using System.Text.Json.Serialization;
using Discord;

namespace Monody.Module.AIChat.Models;

internal class DiscordUserPrompt
{
    private readonly JsonSerializerOptions _jsonOptions = new ()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public DiscordPromptContext DiscordContext { get; }

    public string Prompt { get; }

    public DiscordUserPrompt(IUser user, string prompt)
    {
        Prompt = prompt.Trim();

        DiscordContext = new DiscordPromptContext(user);
    }

    public override string ToString()
    {
        return JsonSerializer.Serialize(this, _jsonOptions);
    }
}

internal class DiscordPromptContext
{
    public DiscordPromptContextUser User { get; }

    public DiscordPromptContext(IUser user)
    {
        User = new DiscordPromptContextUser(user);
    }
}

internal class DiscordPromptContextUser
{
    public ulong Id { get; }

    public string Username { get; }

    public DiscordPromptContextUser(IUser user)
    {
        Id = user.Id;
        Username = user.Username;
    }
}
