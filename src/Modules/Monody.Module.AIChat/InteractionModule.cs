using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using OpenAI.Images;

namespace Monody.Module.AIChat;

[Group("slop", "Slop bridge")]
public class InteractionModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly AIChatService _aiChatService;
    private readonly ILogger _logger;

    private static readonly HttpClient _httpClient = new();

    public InteractionModule(AIChatService aiChatService, ILogger<InteractionModule> logger)
    {
        _aiChatService = aiChatService;
        _logger = logger;
    }

    [SlashCommand("ask", "Ask ChatGPT and get an answer")]
    [CommandContextType(InteractionContextType.PrivateChannel, InteractionContextType.BotDm, InteractionContextType.Guild)]
    public async Task AskAsync(
        [Summary("Prompt", "What do you want to ask?")]
        [MaxLength(1800)]
        string prompt,
        [Summary("LookbackCount", "Optional: include last N messages from this channel (1–100)")]
        [MinValue(1), MaxValue(100)]
        int? lookbackCount = null,
        bool? ephemeral = false
        )
    {
        var channel = Context.Interaction?.InteractionChannel ?? Context.Interaction?.Channel ?? Context.Channel;

        if (lookbackCount > 0 && channel == null)
        {
            await RespondAsync("Insufficient permissions. Unable to perform message lookback.", ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: ephemeral.Value);

        ChatCompletion completion;
        try
        {
            completion = await _aiChatService.GetChatCompletionAsync(Context.Guild, channel, Context.User, prompt, lookbackCount.GetValueOrDefault());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to complete interaction");
            await FollowupAsync("Sorry — the prompt request failed", ephemeral: ephemeral.Value);
            return;
        }

        var text = completion?.Content?[0]?.Text?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            await FollowupAsync("I didn’t get any text back from the model.", ephemeral: ephemeral.Value);
            return;
        }

        // Discord messages cap at 2000 chars; trim if needed
        const int cap = 1900; // leave headroom for code fences etc.
        if (text.Length > cap)
        {
            text = text[..cap] + "…";
        }

        await FollowupAsync(text);
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
}