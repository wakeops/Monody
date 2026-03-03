using System.ComponentModel.DataAnnotations;

namespace Monody.AI.Options;

public sealed class OpenAIConfiguration
{
    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; set; }

    public string ChatModel { get; set; } = "gpt-4.1-mini";

    public string ImageModel { get; set; } = "dall-e-3";
}
