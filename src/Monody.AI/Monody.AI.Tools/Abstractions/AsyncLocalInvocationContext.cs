using System;
using System.Threading;

namespace Monody.AI.Tools.Abstractions;

/// <summary>
/// Flows the invoking user down into tool calls without threading it through the model's
/// arguments. AsyncLocal follows the await chain, so concurrent interactions stay isolated.
/// </summary>
public sealed class AsyncLocalInvocationContext : IInvocationContext
{
    private static readonly AsyncLocal<Scope> _current = new();

    public ulong? UserId => _current.Value?.UserId;

    public ulong? ChannelId => _current.Value?.ChannelId;

    public IDisposable BeginScope(ulong userId, ulong? channelId)
    {
        var previous = _current.Value;
        _current.Value = new Scope(userId, channelId);

        return new Restore(() => _current.Value = previous);
    }

    private sealed record Scope(ulong UserId, ulong? ChannelId);

    private sealed class Restore : IDisposable
    {
        private readonly Action _onDispose;

        public Restore(Action onDispose) => _onDispose = onDispose;

        public void Dispose() => _onDispose();
    }
}
