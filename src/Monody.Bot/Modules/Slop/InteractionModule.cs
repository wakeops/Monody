using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Monody.Bot.Modules.Slop.Modals;
using Monody.Bot.Modules.Slop.Models;
using Monody.Bot.Modules.Slop.Utils;

namespace Monody.Bot.Modules.Slop;

[Group("slop", "Slop bridge")]
public class InteractionModule : InteractionModuleBase<SocketInteractionContext>
{
    private const string LostContextMessage = "Sorry, I lost this conversation's context.";

    // Discord messages cap at 2000 characters; leave room for the ellipsis.
    private const int MaxMessageLength = 1950;

    private static readonly HttpClient _httpClient = new();

    private readonly AIChatService _aiChatService;
    private readonly ConversationStore _conversationStore;
    private readonly ILogger _logger;

    public InteractionModule(AIChatService aiChatService, ConversationStore conversationStore, ILogger<InteractionModule> logger)
    {
        _aiChatService = aiChatService;
        _conversationStore = conversationStore;
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
        if (_conversationStore.Get(originInteractionId) is null)
        {
            await RespondAsync(LostContextMessage, ephemeral: true);
            return;
        }

        await RespondWithModalAsync<SlopFollowupModal>($"monody_followup_modal:{originInteractionId}:{isEphemeral}");
    }

    [ModalInteraction("monody_followup_modal:*:*", true)]
    public async Task Ask_HandleModalAsync(ulong originInteractionId, bool isEphemeral, SlopFollowupModal modal)
    {
        if (_conversationStore.Get(originInteractionId) is null)
        {
            await RespondAsync(LostContextMessage, ephemeral: true);
            return;
        }

        await DeferAsync();

        await ExecuteChatCompletionAsync(originInteractionId, isEphemeral, modal.FollowupText);

        // Retire the follow-up button now that this round has been answered.
        await ModifyOriginalResponseAsync(f => f.Components = new ComponentBuilder().Build());
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

    private static string Truncate(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return text.Length > MaxMessageLength ? text[..MaxMessageLength] + "\u2026" : text;
    }
}
