using System.Threading.Tasks;
using OpenAI.Chat;

namespace Monody.Module.AIChat.Tools;

internal interface IChatToolBase
{
    public string Name { get; }

    public string SystemDescription { get; }

    public ChatTool Tool { get; }

    public Task<ToolChatMessage> ExecuteAsync(ChatToolCall toolFn);
}
