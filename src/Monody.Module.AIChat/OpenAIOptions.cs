using System.ComponentModel.DataAnnotations;

namespace Monody.Module.AIChat;

public sealed class OpenAIOptions
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "OpenAI:ApiKey is required")]
    public string ApiKey { get; set; }

    public string ChatModel { get; set; }

    public string ImageModel { get; set; }
}
