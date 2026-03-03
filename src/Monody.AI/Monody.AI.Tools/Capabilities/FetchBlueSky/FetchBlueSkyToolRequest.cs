using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Monody.AI.Tools.Capabilities.FetchBlueSky;

public class FetchBlueSkyToolRequest
{
    [Description("The full URL to fetch (http or https).")]
    [Required]
    public string Url { get; set; }
}
