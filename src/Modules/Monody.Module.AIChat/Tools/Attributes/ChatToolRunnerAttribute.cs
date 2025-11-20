using System;

namespace Monody.Module.AIChat.Tools.Attributes;

[AttributeUsage(AttributeTargets.Class)]
internal class ChatToolRunnerAttribute : Attribute
{
    public string Name { get; set; }

    public string SystemDescription { get; set; }
}
