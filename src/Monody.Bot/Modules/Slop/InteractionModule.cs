using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Monody.Bot.Modules.Slop.Modals;
using Monody.Bot.Modules.Slop.Models;
using Monody.Bot.Modules.Slop.Utils;
using Monody.Data;
using Monody.Data.Entities;

namespace Monody.Bot.Modules.Slop;

[Group("slop", "Slop bridge")]
[IntegrationType(ApplicationIntegrationType.UserInstall, ApplicationIntegrationType.GuildInstall)]
public class InteractionModule : InteractionModuleBase<SocketInteractionContext>
{
    private const string LostContextMessage = "Sorry, I lost this conversation's context.";

    // Discord messages cap at 2000 characters; leave room for the ellipsis.
    private const int MaxMessageLength = 1950;

    private static readonly HttpClient _httpClient = new();

    private const string MemorySelectMenuId = "monody_memory_delete";
    private const string MemoryDeleteAllButtonId = "monody_memory_delete_all";

    private readonly AIChatService _aiChatService;
    private readonly ConversationStore _conversationStore;
    private readonly MemoryStore _memoryStore;
    private readonly ILogger _logger;

    public InteractionModule(
        AIChatService aiChatService,
        ConversationStore conversationStore,
        MemoryStore memoryStore,
        ILogger<InteractionModule> logger)
    {
        _aiChatService = aiChatService;
        _conversationStore = conversationStore;
        _memoryStore = memoryStore;
        _logger = logger;
    }

    [SlashCommand("ask", "Ask ChatGPT and get an answer")]
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.BotDm, InteractionContextType.Guild)]
    public async Task AskAsync(
        [Summary("Prompt", "What do you want to ask?")]
        [MaxLength(1800)]
        string prompt,
        bool ephemeral = false)
    {
        await DeferAsync(ephemeral: ephemeral);

        await ExecuteChatCompletionAsync(Context.Interaction.Id, ephemeral, prompt);
    }

    [ComponentInteraction("monody_followup:*:*", true)]
    public async Task Ask_OpenModalAsync(ulong originInteractionId, bool isEphemeral)
    {
        if (!await _conversationStore.ExistsAsync(originInteractionId))
        {
            await RespondAsync(LostContextMessage, ephemeral: true);
            return;
        }

        await RespondWithModalAsync<SlopFollowupModal>($"monody_followup_modal:{originInteractionId}:{isEphemeral}");
    }

    [ModalInteraction("monody_followup_modal:*:*", true)]
    public async Task Ask_HandleModalAsync(ulong originInteractionId, bool isEphemeral, SlopFollowupModal modal)
    {
        if (!await _conversationStore.ExistsAsync(originInteractionId))
        {
            await RespondAsync(LostContextMessage, ephemeral: true);
            return;
        }

        // Match the thread: an ephemeral conversation must not become public just because the
        // follow-up came through a modal.
        await DeferAsync(ephemeral: isEphemeral);

        await ExecuteChatCompletionAsync(originInteractionId, isEphemeral, modal.FollowupText);

        // The answer goes out as a followup, so resolve the deferred placeholder this modal
        // created rather than leaving it spinning.
        await ModifyOriginalResponseAsync(f =>
        {
            f.Content = "Answered below.";
            f.Components = new ComponentBuilder().Build();
        });
    }

    [SlashCommand("image", "Ask ChatGPT and get an image")]
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.BotDm, InteractionContextType.Guild)]
    public async Task ImageAsync(
        [Summary("Prompt", "What do you want to generate?")]
        [MaxLength(800)]
        string prompt,
        bool ephemeral = false)
    {
        await DeferAsync(ephemeral: ephemeral);

        Uri imageUri;
        try
        {
            imageUri = await _aiChatService.GetImageGenerationAsync(prompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to complete interaction");
            await FollowupAsync("Image generation failed", ephemeral: ephemeral);
            return;
        }

        try
        {
            using var stream = await _httpClient.GetStreamAsync(imageUri);

            var extension = Path.GetExtension(imageUri.LocalPath);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".jpg";
            }

            var filename = $"monody_{Context.User.Id}_{DateTime.UtcNow:yyyyMMddHHmmss}{extension.ToLowerInvariant()}";

            await FollowupWithFileAsync(stream, filename, text: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to complete interaction");
            await FollowupAsync($"Failed to fetch or upload the image: `{ex.Message}`");
        }
    }

    private async Task ExecuteChatCompletionAsync(ulong interactionId, bool isEphemeral, string prompt)
    {
        DiscordCompletionResponse completion;
        try
        {
            completion = await _aiChatService.GetChatCompletionAsync(
                interactionId, Context.Guild, Context.Interaction?.InteractionChannel, Context.User, prompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to complete interaction");
            await FollowupAsync("Sorry — the prompt request failed", ephemeral: isEphemeral);
            return;
        }

        // Strict structured outputs force every property to be present, so "unused" arrives as
        // an empty string rather than null.
        var content = Truncate(completion?.Text);

        var embed = completion?.Kind == DiscordResponseKind.Embed
            ? DiscordEmbedFactory.TryBuild(completion.Embed)
            : null;

        // Text is sent either way: alongside an embed it reads as a short lead-in, and if the
        // model asked for an embed but supplied nothing renderable it is all we have left.
        var text = content;

        if (embed is null && text is null)
        {
            await FollowupAsync("I didn't get any text back from the model.", ephemeral: isEphemeral);
            return;
        }

        var components = new ComponentBuilder()
            .WithButton(
                label: "Follow up",
                customId: $"monody_followup:{interactionId}:{isEphemeral}",
                style: ButtonStyle.Primary)
            .Build();

        if (isEphemeral)
        {
            await FollowupAsync(text: text, embed: embed, components: components, ephemeral: true);
            return;
        }

        // A public reply can't carry the follow-up button, since anyone could press it;
        // send the button separately, visible only to the user who asked.
        await FollowupAsync(text: text, embed: embed, ephemeral: false);
        await FollowupAsync("You can follow up on this reply here:", components: components, ephemeral: true);
    }

    [SlashCommand("memories", "See and manage what Monody remembers about you.")]
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.BotDm, InteractionContextType.Guild)]
    public async Task ShowMemoriesAsync()
    {
        await DeferAsync(ephemeral: true);

        await ShowCurrentAsync();
    }

    [ComponentInteraction(MemorySelectMenuId, true)]
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

    [ComponentInteraction(MemoryDeleteAllButtonId, true)]
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

        var embed = BuildMemoriesEmbed(memories, notice);
        var components = BuildMemoryComponents(memories);

        await ModifyOriginalResponseAsync(properties =>
        {
            properties.Content = null;
            properties.Embed = embed;
            properties.Components = components;
        });
    }

    private static Embed BuildMemoriesEmbed(IReadOnlyList<UserMemory> memories, string notice)
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
                DescribeCategory(group.Key),
                string.Join('\n', group.Select(m => $"• {m.Content}")),
                inline: false);
        }

        return embed.Build();
    }

    private static MessageComponent BuildMemoryComponents(IReadOnlyList<UserMemory> memories)
    {
        if (memories.Count == 0)
        {
            return new ComponentBuilder().Build();
        }

        // A select menu rather than a modal: deleting is picking from a list, not typing text.
        var menu = new SelectMenuBuilder()
            .WithCustomId(MemorySelectMenuId)
            .WithPlaceholder("Select memories to forget…")
            .WithMinValues(1)
            .WithMaxValues(memories.Count);

        foreach (var memory in memories)
        {
            menu.AddOption(
                label: TruncateLabel(memory.Content, SelectMenuOptionBuilder.MaxSelectLabelLength),
                value: memory.Id.ToString(),
                description: DescribeCategory(memory.Category));
        }

        return new ComponentBuilder()
            .WithSelectMenu(menu)
            .WithButton("Forget everything", MemoryDeleteAllButtonId, ButtonStyle.Danger, row: 1)
            .Build();
    }

    private static string DescribeCategory(MemoryCategory category) => category switch
    {
        MemoryCategory.Name => "Name",
        MemoryCategory.Location => "Location",
        MemoryCategory.TimeZone => "Time zone",
        MemoryCategory.Preference => "Preferences",
        _ => category.ToString()
    };

    private static string TruncateLabel(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..(maxLength - 1)] + "…";

    private static string Truncate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return text.Length > MaxMessageLength ? text[..MaxMessageLength] + "\u2026" : text;
    }
}
