using System;
using System.Net.Http;
using System.Threading.Tasks;
using Monody.Module.AIChat.Tools.Abstractions;
using Monody.Module.AIChat.Tools.Attributes;
using OpenAI.Chat;

namespace Monody.Module.AIChat.Tools.Definitions.FetchUrl;

[ChatToolRunner(Name = "fetch_url", SystemDescription = "You have access to a tool named fetch_url that returns the raw content of a URL. Whenever the user asks you to summarize, analyze, or read from a URL, you should call fetch_url with that URL before answering.")]
[ChatToolFunction("Fetches the raw content of a given URL for the assistant to analyze.")]
internal class FetchUrlTool : ChatToolRunner<FetchUrlToolRequest>
{
    private readonly HttpClient _httpClient;

    public FetchUrlTool()
    {
        _httpClient = new HttpClient();
    }

    public override async Task<ToolChatMessage> ExecuteAsync(ChatToolCall toolFn, FetchUrlToolRequest args)
    {
        if (args is null || string.IsNullOrWhiteSpace(args.Url))
        {
            // Defensive: if bad args, provide an error payload
            return ChatMessage.CreateToolMessage(toolFn.Id, "Error: Missing or invalid URL.");
        }

        // Fetch the URL server-side
        var resp = await _httpClient.GetAsync(args.Url);

        if (!resp.IsSuccessStatusCode)
        {
           return ChatMessage.CreateToolMessage(toolFn.Id, $"Error fetching URL ({(int)resp.StatusCode} {resp.ReasonPhrase}).");
        }

        string body = await resp.Content.ReadAsStringAsync();

        // Truncate to avoid huge payloads
        const int maxLen = 20_000;
        if (body.Length > maxLen)
        {
            body = string.Concat(body.AsSpan(0, maxLen), "\n\n[Truncated]");
        }

        // Send the tool result back to the model
        return ChatMessage.CreateToolMessage(toolFn.Id, body);
    }
}
