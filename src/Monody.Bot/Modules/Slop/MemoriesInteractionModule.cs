using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Monody.Data;
using Monody.Data.Entities;

namespace Monody.Bot.Modules.Slop;

/// <summary>
/// Lets a user see and delete what the assistant has remembered about them.
/// </summary>
/// <remarks>
/// Every response is ephemeral and every query is filtered by <c>Context.User.Id</c>, so one
/// person's memories are never visible or deletable by another - including through a stale
/// component on someone else's screen, since the delete re-filters by the clicking user.
/// </remarks>
[Group("slop", "Slop bridge")]
public class MemoriesInteractionModule : InteractionModuleBase<SocketInteractionContext>
{
    private const string SelectMenuId = "monody_memory_delete";
    private const string DeleteAllButtonId = "monody_memory_delete_all";

    private readonly MemoryStore _memoryStore;

    public MemoriesInteractionModule(MemoryStore memoryStore)
    {
        _memoryStore = memoryStore;
    }

    [SlashCommand("memories", "See and manage what Monody remembers about you.")]
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.BotDm, InteractionContextType.Guild)]
    public async Task ShowMemoriesAsync()
    {
        await DeferAsync(ephemeral: true);

        await ShowCurrentAsync();
    }

    [ComponentInteraction(SelectMenuId, true)]
    public async Task DeleteSelectedAsync(string[] selectedIds)
    {
        await DeferAsync(ephemeral: true);

        var ids = selectedIds
            .Select(id => int.TryParse(id, out var parsed) ? parsed : (int?)null)
            .Where(id => id.HasValue)
            .Select(id => id.Value);

        var deleted = await _memoryStore.ForgetAsync(Context.User.Id, ids);

        await ShowCurrentAsync(deleted == 1 ? "Forgot 1 memory." : $"Forgot {deleted} memories.");
    }

    [ComponentInteraction(DeleteAllButtonId, true)]
    public async Task DeleteAllAsync()
    {
        await DeferAsync(ephemeral: true);

        var deleted = await _memoryStore.ForgetAllAsync(Context.User.Id);

        await ShowCurrentAsync(deleted == 0 ? "There was nothing to forget." : $"Forgot everything ({deleted}).");
    }

    /// <summary>Re-renders the ephemeral message from the store, so it always shows current state.</summary>
    private async Task ShowCurrentAsync(string notice = null)
    {
        var memories = await _memoryStore.GetAsync(Context.User.Id);

        var embed = BuildEmbed(memories, notice);
        var components = BuildComponents(memories);

        await ModifyOriginalResponseAsync(properties =>
        {
            properties.Content = null;
            properties.Embed = embed;
            properties.Components = components;
        });
    }

    private Embed BuildEmbed(IReadOnlyList<UserMemory> memories, string notice)
    {
        var embed = new EmbedBuilder()
            .WithTitle("What Monody remembers about you")
            .WithColor(new Color(MonodyConstants.DefaultEmbedColor))
            .WithFooter("Only you can see this.");

        if (memories.Count == 0)
        {
            embed.WithDescription(
                notice is null
                    ? "Nothing yet. Monody saves a fact when you mention something lasting, like where you live."
                    : $"{notice}\n\nNothing is stored now.");

            return embed.Build();
        }

        if (notice is not null)
        {
            embed.WithDescription(notice);
        }

        foreach (var group in memories.GroupBy(m => m.Category))
        {
            embed.AddField(
                Describe(group.Key),
                string.Join('\n', group.Select(m => $"• {m.Content}")),
                inline: false);
        }

        return embed.Build();
    }

    private MessageComponent BuildComponents(IReadOnlyList<UserMemory> memories)
    {
        if (memories.Count == 0)
        {
            return new ComponentBuilder().Build();
        }

        // A select menu rather than a modal: deleting is picking from a list, not typing text.
        var menu = new SelectMenuBuilder()
            .WithCustomId(SelectMenuId)
            .WithPlaceholder("Select memories to forget…")
            .WithMinValues(1)
            .WithMaxValues(memories.Count);

        foreach (var memory in memories)
        {
            menu.AddOption(
                label: Truncate(memory.Content, SelectMenuOptionBuilder.MaxSelectLabelLength),
                value: memory.Id.ToString(),
                description: Describe(memory.Category));
        }

        return new ComponentBuilder()
            .WithSelectMenu(menu)
            .WithButton("Forget everything", DeleteAllButtonId, ButtonStyle.Danger, row: 1)
            .Build();
    }

    private static string Describe(MemoryCategory category) => category switch
    {
        MemoryCategory.Name => "Name",
        MemoryCategory.Location => "Location",
        MemoryCategory.TimeZone => "Time zone",
        MemoryCategory.Preference => "Preferences",
        _ => category.ToString()
    };

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";
}
