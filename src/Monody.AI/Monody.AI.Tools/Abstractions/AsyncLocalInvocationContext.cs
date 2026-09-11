using Discord.WebSocket;

namespace Monody.AI.Tools.Abstractions;

/// <summary>
/// Flows the invoking user down into tool calls without threading it through the model's
/// arguments. AsyncLocal follows the await chain, so concurrent interactions stay isolated.
/// </summary>
public sealed class AsyncLocalInvocationContext : IInvocationContext
{
    private static readonly AsyncLocal<Scope> _current = new();

    public SocketInteraction Interaction => _current.Value?.Interaction;

    public IDisposable BeginScope(SocketInteraction interactionContext)
    {
        var previous = _current.Value;
        _current.Value = new Scope(interactionContext);

        return new Restore(() => _current.Value = previous);
    }

    private sealed record Scope(SocketInteraction Interaction);

    private sealed class Restore : IDisposable
    {
        private readonly Action _onDispose;

        public Restore(Action onDispose) => _onDispose = onDispose;

        public void Dispose() => _onDispose();
    }
}
