using System.ComponentModel.DataAnnotations;

namespace Monody.AI.Options;

internal sealed class AIOptions
{
    [Required]
    public string ChatCompletionProvider { get; } = "openai";
}
