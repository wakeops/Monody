using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Monody.Data;

namespace Monody.AI.Tools.Capabilities.Memory;

public sealed class RememberToolRequest
{
    [Description(
        "A short lowercase kebab-case id for this topic, e.g. 'home-location' or 'coffee-preference'. " +
        "If this slug already exists for the user, its description and content are replaced; use the " +
        "same slug again to update a topic rather than creating a near-duplicate one.")]
    [Required]
    [MaxLength(DataConstants.MaxSlugLength)]
    public string Slug { get; set; }

    [Description(
        "One short line summarizing this topic, shown in the index before the full content is " +
        "loaded. Keep it under 80 characters, e.g. 'Where the user lives'.")]
    [Required]
    [MaxLength(DataConstants.MaxMemoryDescriptionLength)]
    public string Description { get; set; }

    [Description(
        "The full note for this topic, written for your own later reading. Only for lasting facts " +
        "the user has volunteered. Never store passing details, one-off questions, opinions about " +
        "others, or anything sensitive.")]
    [Required]
    [MaxLength(DataConstants.MaxMemoryContentLength)]
    public string Content { get; set; }
}

public sealed class RememberToolResponse
{
    [Description("Whether the topic was saved.")]
    public bool Saved { get; set; }

    [Description("What happened, to relay to the user if it is worth mentioning.")]
    public string Outcome { get; set; }
}

public sealed class RecallIndexToolResponse
{
    [Description(
        "Every remembered topic's slug and one-line description for the current user, cheapest to " +
        "scan first. Call recall_topic with a slug to load its full content. Empty when nothing is stored.")]
    public List<MemoryIndexEntry> Topics { get; set; } = [];
}

public sealed class MemoryIndexEntry
{
    [Description("Identifier for this topic. Pass it to forget to remove it.")]
    public int Id { get; set; }

    [Description("The topic's slug. Reuse this in remember to update the topic instead of creating a new one.")]
    public string Slug { get; set; }

    [Description("One-line summary of the topic.")]
    public string Description { get; set; }
}

public sealed class RecallTopicToolRequest
{
    [Description("The slug of the topic to load, taken from a recall_index result.")]
    [Required]
    public string Slug { get; set; }
}

public sealed class RecallTopicToolResponse
{
    [Description("Whether a topic with that slug exists for the current user.")]
    public bool Found { get; set; }

    [Description("The topic's full content, or empty if not found.")]
    public string Content { get; set; }
}

public sealed class ForgetToolRequest
{
    [Description("The Id of the topic to remove, taken from a recall_index result.")]
    [Required]
    public int MemoryId { get; set; }
}

public sealed class ForgetToolResponse
{
    [Description("Whether a topic was removed.")]
    public bool Forgotten { get; set; }

    [Description("What happened, to relay if it is worth mentioning.")]
    public string Outcome { get; set; }
}
