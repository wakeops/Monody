using System;

namespace Monody.Module.AIChat.Tools.Attributes;

[AttributeUsage(AttributeTargets.Class)]
internal class ChatToolFunctionAttribute : Attribute
{
    public string Description { get; }

    public ChatToolFunctionAttribute(string Description)
    {
        this.Description = Description;
    }
}
