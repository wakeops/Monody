using System;
using System.Threading.Tasks;
using Monody.Module.AIChat.Tools.Abstractions;
using Monody.Module.AIChat.Tools.Attributes;
using OpenAI.Chat;

namespace Monody.Module.AIChat.Tools.Definitions.FetchBlueSky;

[ChatToolRunner(Name = "fetch_bluesky", SystemDescription = "You have access to a tool named fetch_bluesky that returns information about a bsky post. Whenever the user asks you to summarize, analyze, or read from a bsky.app URL, you should call fetch_bluesky with that URL before answering.")]
[ChatToolFunction("Fetches the raw content of a given URL for the assistant to analyze.")]
internal class FetchBlueSkyTool : ChatToolRunner<FetchBlueSkyToolRequest>
{
    private readonly BlueSkyService _blueSkyService;

    public FetchBlueSkyTool(BlueSkyService blueSkyService)
    {
        _blueSkyService = blueSkyService;
    }

    public override async Task<ToolChatMessage> ExecuteAsync(ChatToolCall toolFn, FetchBlueSkyToolRequest args)
    {
        if (args is null || string.IsNullOrWhiteSpace(args.Url))
        {
            // Defensive: if bad args, provide an error payload
            return ChatMessage.CreateToolMessage(toolFn.Id, "Error: Missing or invalid URL.");
        }

        if (!IsBlueskyUrl(args.Url))
        {
            throw new ArgumentException("URL is not a valid bsky.app URL.", nameof(args.Url));
        }

        var content = await _blueSkyService.FetchThreadTextAsync(args.Url);

        // Send the tool result back to the model
        return ChatMessage.CreateToolMessage(toolFn.Id, content);
    }

    private static bool IsBlueskyUrl(string url)
        => Uri.TryCreate(url, UriKind.Absolute, out var uri)
           && uri.Host is "bsky.app";
}
