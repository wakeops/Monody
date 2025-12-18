using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using Monody.AI.Domain.Models;
using Monody.Bot.Modules.Slop.Modals;
using Monody.Bot.Modules.Slop.Models;

namespace Monody.Bot.Modules.Slop;

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

        await ModifyOriginalResponseAsync(f => f.Components = new ComponentBuilder().Build());
    }

    [SlashCommand("image", "Ask ChatGPT and get an image")]
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.BotDm, InteractionContextType.Guild)]
    public async Task ImageAsync(
        [Summary("Prompt", "What do you want to generate?")]
        [MaxLength(800)]
        string prompt,
        bool? ephemeral = false
        )
    {
        await DeferAsync(ephemeral: ephemeral.Value);

        ImageGenerationResult img;
        try
        {
            img = await _aiChatService.GetImageGenerationAsync(prompt);
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

        DiscordCompletionResponse completion;
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

        var components = new ComponentBuilder()
            .WithButton(
                label: "Follow up",
                customId: $"monody_followup:{interactionId}:{isEphemeral}",
                style: ButtonStyle.Primary);

        string text = null;
        Embed embed = null;

        if (completion?.Kind == DiscordResponseKind.Embed && completion?.Embed != null)
        {
            embed = BuildEmbedFromResponse(completion.Embed);
        }
        else if (completion?.Kind == DiscordResponseKind.Text && !string.IsNullOrEmpty(completion?.Text))
        {
            text = completion?.Text;

            // Discord messages cap at 2000 chars; trim if needed
            const int cap = 1950;
            if (text.Length > cap)
            {
                text = text[..cap] + "…";
            }
        }
        else
        {
            await FollowupAsync("I didn’t get any text back from the model.", ephemeral: isEphemeral);
            return;
        }

        if (isEphemeral)
        {
            await FollowupAsync(text: text, embed: embed, components: components.Build(), ephemeral: true);
        }
        else
        {
            await FollowupAsync(text: text, embed: embed, ephemeral: false);
            await FollowupAsync("You can follow up on this reply here:", components: components.Build(), ephemeral: true);
        }
    }

    public static Embed BuildEmbedFromResponse(DiscordEmbed model)
    {
        var builder = new EmbedBuilder()
            .WithTitle(model.Title)
            .WithDescription(model.Description)
            .WithColor(new Color(MonodyConstants.DefaultEmbedColor));

        if (model.Fields != null)
        {
            foreach (var field in model.Fields)
            {
                builder.AddField(field.Name, field.Value, field.Inline);
            }
        }

        if (model.Footer != null)
        {
            var fb = new EmbedFooterBuilder()
                .WithText(model.Footer.Text)
                .WithIconUrl(model.Footer.IconUrl);

            builder.WithFooter(fb);
        }

        return builder.Build();
    }
}