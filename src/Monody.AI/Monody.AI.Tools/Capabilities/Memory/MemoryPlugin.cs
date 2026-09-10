using System.ComponentModel;
using Microsoft.SemanticKernel;
using Monody.AI.Tools.Abstractions;
using Monody.Data.Stores;

namespace Monody.AI.Tools.Capabilities.Memory;

public sealed class MemoryPlugin(IMemoryStore memoryStore, IInvocationContext invocationContext)
{
    [KernelFunction("remember")]
    [Description(
        "Saves or updates one durable topic about the current user, for use in later conversations. " +
        "Call recall_index first: if an existing slug already covers this topic, reuse it so the " +
        "topic is updated in place rather than duplicated. Only for lasting facts they have " +
        "volunteered - their name, where they live, their time zone, or a standing preference. " +
        "Never store passing details, one-off questions, opinions about others, or anything sensitive.")]
    public async Task<RememberToolResponse> RememberAsync(RememberToolRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Slug);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Description);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Content);

        var userId = RequireUserId();

        var result = await memoryStore.RememberAsync(userId, request.Slug, request.Description, request.Content, cancellationToken);

        return new RememberToolResponse
        {
            Saved = result.Success,
            Outcome = DescribeOutcome(result)
        };
    }

    [KernelFunction("recall_index")]
    [Description(
        "Returns the slug and one-line description of every topic remembered about the current " +
        "user, without their full content. Cheap enough to call before most answers that might " +
        "depend on who the user is. Follow up with recall_topic for any topic whose description " +
        "looks relevant.")]
    public async Task<RecallIndexToolResponse> RecallIndexAsync(CancellationToken cancellationToken = default)
    {
        var userId = RequireUserId();

        var index = await memoryStore.GetIndexAsync(userId, cancellationToken);

        return new RecallIndexToolResponse
        {
            Topics = [.. index.Select(m => new MemoryIndexEntry
            {
                Id = m.Id,
                Slug = m.Slug,
                Description = m.Description
            })]
        };
    }

    [KernelFunction("recall_topic")]
    [Description(
        "Returns the full content of one remembered topic about the current user, by slug. Call " +
        "recall_index first to find the slug.")]
    public async Task<RecallTopicToolResponse> RecallTopicAsync(RecallTopicToolRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.Slug);

        var userId = RequireUserId();

        var topic = await memoryStore.GetTopicAsync(userId, request.Slug, cancellationToken);

        return new RecallTopicToolResponse
        {
            Found = topic is not null,
            Content = topic?.Content ?? ""
        };
    }

    [KernelFunction("forget")]
    [Description(
        "Removes one remembered topic about the current user. Use this when they ask you to forget " +
        "something, or a topic no longer holds and isn't just being updated. Call recall_index first " +
        "to get the Id - if the topic is simply changing, prefer calling remember with the same slug " +
        "instead of forgetting and re-saving it.")]
    public async Task<ForgetToolResponse> ForgetAsync(ForgetToolRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var userId = RequireUserId();

        var removed = await memoryStore.ForgetAsync(userId, [request.MemoryId], cancellationToken);

        return new ForgetToolResponse
        {
            Forgotten = removed > 0,
            Outcome = removed > 0
                ? "Forgotten."
                : "No topic with that Id belongs to this user; call recall_index for the current list."
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

        return result.Replaced ? "Saved, updating the existing topic." : "Saved as a new topic.";
    }
}
