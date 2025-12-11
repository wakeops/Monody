using System.ComponentModel.DataAnnotations;

namespace Monody.AI.Provider.OpenAI;

public class OpenAIConfiguration
{
    [Required(AllowEmptyStrings = false)]
    public string ApiKey { get; set; }
    
    public string ChatModel { get; set; }
    
    public string ImageModel { get; set; }
}
