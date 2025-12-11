namespace Monody.AI.Domain.Models;

public sealed class ChatMessageDto
{
    public ChatRole Role { get; init; }

    public string Content { get; init; }

    public string ToolName { get; init; }

    public string ToolCallId { get; init; }
}
