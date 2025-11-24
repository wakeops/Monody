using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Monody.Module.AIChat.Modals;
using OpenAI.Chat;
using OpenAI.Images;

namespace Monody.Module.AIChat;

[Group("slop", "Slop bridge")]
public class InteractionModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly AIChatService _aiChatService;
    private readonly ConversationStore _conversationStore;
    private readonly ILogger _logger;

    private static readonly HttpClient _httpClient = new();

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
        bool? ephemeral = false
        )
    {
        await DeferAsync(ephemeral: ephemeral.Value);

        var interactionId = Context.Interaction.Id;

        await ExecuteChatCompletion(interactionId, ephemeral.Value, prompt);
    }

    [ComponentInteraction("monody_followup:*:*", true)]
    public async Task Ask_OpenModalAsync(string originInteractionId, bool isEphemeral)
    {
        var conversation = _conversationStore.GetConversation(originInteractionId);
        if (conversation is null)
        {
            await RespondAsync("Sorry, I lost this conversation’s context.", ephemeral: true);
            return;
        }

        await RespondWithModalAsync<SlopFollowupModal>($"monody_followup_modal:{originInteractionId}:{isEphemeral}");
    }

    [ModalInteraction("monody_followup_modal:*:*", true)]
    public async Task Ask_HandleModalAsync(ulong originInteractionId, bool isEphemeral, SlopFollowupModal modal)
    {
        var conversation = _conversationStore.GetConversation(originInteractionId.ToString());
        if (conversation is null)
        {
            await RespondAsync("Sorry, I lost this conversation’s context.", ephemeral: true);
            return;
        }

        await DeferAsync();

        await ExecuteChatCompletion(originInteractionId, isEphemeral, modal.FollowupText);

        if (isEphemeral)
        {
            await ModifyOriginalResponseAsync(f => f.Components = new ComponentBuilder().Build());
        }
        else
        {
            await DeleteOriginalResponseAsync();
        }
    }

    [SlashCommand("image", "Ask ChatGPT and get an image")]
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.BotDm, InteractionContextType.Guild)]
    public async Task ImageAsync(
        [Summary("Prompt", "What do you want to generate?")]
        [MaxLength(800)]
        string prompt,
        [Summary("Size", "Image size")]
        [Choice("1024×1024", "1024"), Choice("512×512", "512"), Choice("256×256", "256")]
        string size = "1024",
        bool? ephemeral = false
        )
    {
        await DeferAsync(ephemeral: ephemeral.Value);

        var genSize = size switch
        {
            "256" => GeneratedImageSize.W256xH256,
            "512" => GeneratedImageSize.W512xH512,
            _ => GeneratedImageSize.W1024xH1024
        };

        GeneratedImage img;
        try
        {
            img = await _aiChatService.GetImageGenerationAsync(prompt, genSize);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to complete interaction");

            await FollowupAsync("Image generation failed", ephemeral: ephemeral.Value);
            return;
        }

        try
        {
            using var stream = await _httpClient.GetStreamAsync(img.ImageUri);

            var ext = Path.GetExtension(img.ImageUri.LocalPath);
            if (string.IsNullOrWhiteSpace(ext))
            {
                ext = ".jpg";
            }

            var userId = Context.User.Id;
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var filename = $"monody_{userId}_{timestamp}{ext.ToLowerInvariant()}";

            await FollowupWithFileAsync(stream, filename, text: null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to complete interaction");
            await FollowupAsync($"Failed to fetch or upload the image: `{ex.Message}`");
        }
    }

    private async Task ExecuteChatCompletion(ulong interactionId, bool isEphemeral, string prompt)
    {
        var channel = Context.Interaction?.InteractionChannel;

        ChatCompletion completion;
        try
        {
            completion = await _aiChatService.GetChatCompletionAsync(interactionId, Context.Guild, channel, Context.User, prompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to complete interaction");
            await FollowupAsync("Sorry — the prompt request failed", ephemeral: isEphemeral);
            return;
        }

        var text = completion?.Content?[0]?.Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            await FollowupAsync("I didn’t get any text back from the model.", ephemeral: isEphemeral);
            return;
        }

        // Discord messages cap at 2000 chars; trim if needed
        const int cap = 1950;
        if (text.Length > cap)
        {
            text = text[..cap] + "…";
        }

        var components = new ComponentBuilder()
            .WithButton(
                label: "Follow up",
                customId: $"monody_followup:{interactionId}:{isEphemeral}",
                style: ButtonStyle.Primary);

        if (isEphemeral)
        {
            await FollowupAsync(text, components: components.Build(), ephemeral: true);
        }
        else
        {
            await FollowupAsync(text);

            await FollowupAsync("You can follow up on this reply here:", components: components.Build(), ephemeral: true);
        }
    }
}