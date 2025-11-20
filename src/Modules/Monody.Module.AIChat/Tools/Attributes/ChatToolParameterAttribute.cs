using System;

namespace Monody.Module.AIChat.Tools.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
internal class ChatToolParameterAttribute : Attribute
{
    public string Name { get; }

    public string Type { get; }

    public string Description { get; init; }

    public bool Required { get; init; } = false;

    public ChatToolParameterAttribute(string Name, string Type)
    {
        this.Name = Name;
        this.Type = Type;
    }
}
