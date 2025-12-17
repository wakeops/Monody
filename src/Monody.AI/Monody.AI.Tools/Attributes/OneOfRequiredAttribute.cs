using System;

namespace Monody.AI.Tools.Attributes;

public sealed class OneOfRequiredAttribute : Attribute
{
    public int GroupId { get; }

    public OneOfRequiredAttribute(int groupId)
    {
        GroupId = groupId;
    }
}
