using System;

namespace Monody.AI.Tools.Abstractions;

/// <summary>
/// Who the model is currently acting for.
/// </summary>
/// <remarks>
/// Tools that touch per-user data read the id from here rather than taking it as a parameter.
/// A parameter would be chosen by the model, and the model's context contains untrusted text -
/// channel history and fetched web pages - so it could be talked into reading or overwriting
/// somebody else's memories.
/// </remarks>
public interface IInvocationContext
{
    ulong? UserId { get; }

    ulong? ChannelId { get; }

    IDisposable BeginScope(ulong userId, ulong? channelId);
}
