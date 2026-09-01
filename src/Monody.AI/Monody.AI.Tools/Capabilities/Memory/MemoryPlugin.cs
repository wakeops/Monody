using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Monody.AI.Tools.Abstractions;
using Monody.Data;

namespace Monody.AI.Tools.Capabilities.Memory;

/// <summary>
/// Lets the assistant carry a few durable facts about a user between conversations.
/// </summary>
/// <remarks>
/// Neither function takes a user id. The id comes from <see cref="IInvocationContext"/>, which
/// the chat service sets from the Discord interaction, so the model cannot be argued into
/// reading or overwriting another user's memories by text it found in a channel or a web page.
/// </remarks>
public sealed class MemoryPlugin(MemoryStore memoryStore, IInvocationContext invocationContext)
{
    [KernelFunction("remember")]
    [Description(
        "Saves one durable personal fact about the current user so it is available in later " +
        "conversations. Only for lasting facts they have volunteered: their name, where they " +
        "live, their time zone, or a standing preference. Never store passing details, one-off " +
        "questions, opinions about others, or anything sensitive.")]
    public async Task<RememberToolResponse> RememberAsync(RememberToolRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Content);

        var userId = RequireUserId();

        var result = await memoryStore.RememberAsync(userId, request.Category, request.Content, cancellationToken);

        return new RememberToolResponse
        {
            Saved = result.Success,
            Outcome = DescribeOutcome(result)
        };
    }

    [KernelFunction("recall")]
    [Description(
        "Returns what is already remembered about the current user. Call this when the answer " +
        "depends on who they are - their location, units, or how they like to be addressed - " +
        "before asking them for something they may have already told you.")]
    public async Task<RecallToolResponse> RecallAsync(RecallToolRequest request, CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();

        var memories = await memoryStore.GetAsync(userId, cancellationToken);

        return new RecallToolResponse
        {
            Memories = [.. memories.Select(m => new RecalledMemory
            {
                Category = m.Category.ToString(),
                Content = m.Content
            })]
        };
    }

    private ulong RequireUserId() =>
        invocationContext.UserId
        ?? throw new InvalidOperationException("No Discord user is in scope, so memories cannot be read or written.");

    private static string DescribeOutcome(MemoryWriteResult result)
    {
        if (!result.Success)
        {
            return result.Reason;
        }

        if (result.Duplicate)
        {
            return "Already remembered; nothing changed.";
        }

        return result.Replaced ? "Saved, replacing the previous value." : "Saved.";
    }
}
