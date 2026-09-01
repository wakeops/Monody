using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Monody.Data;
using Monody.Data.Entities;

namespace Monody.AI.Tools.Capabilities.Memory;

public sealed class RememberToolRequest
{
    [Description(
        "What kind of fact this is. Name, Location and TimeZone hold one value each and are " +
        "replaced when they change; Preference may have several.")]
    [Required]
    public MemoryCategory Category { get; set; }

    [Description(
        "The fact, written as a short standalone statement in the third person, e.g. " +
        "'Lives in Raleigh, NC' or 'Prefers metric units'. Keep it under 200 characters.")]
    [Required]
    [MaxLength(DataConstants.MaxMemoryLength)]
    public string Content { get; set; }
}

public sealed class RememberToolResponse
{
    [Description("Whether the fact was saved.")]
    public bool Saved { get; set; }

    [Description("What happened, to relay to the user if it is worth mentioning.")]
    public string Outcome { get; set; }
}

public sealed class RecallToolRequest
{
    // Structured outputs require at least one property, and the store is small enough that
    // filtering is not worth the extra failure mode.
    [Description("Unused. Recall always returns everything remembered about the current user.")]
    public string Unused { get; set; }
}

public sealed class RecallToolResponse
{
    [Description("Everything remembered about the current user. Empty when nothing is stored.")]
    public List<RecalledMemory> Memories { get; set; } = [];
}

public sealed class RecalledMemory
{
    [Description("Identifier for this memory. Pass it to forget to remove it.")]
    public int Id { get; set; }

    [Description("Which kind of fact this is.")]
    public string Category { get; set; }

    [Description("The remembered fact.")]
    public string Content { get; set; }
}

public sealed class ForgetToolRequest
{
    [Description("The Id of the memory to remove, taken from a recall result.")]
    [Required]
    public int MemoryId { get; set; }
}

public sealed class ForgetToolResponse
{
    [Description("Whether a memory was removed.")]
    public bool Forgotten { get; set; }

    [Description("What happened, to relay if it is worth mentioning.")]
    public string Outcome { get; set; }
}
