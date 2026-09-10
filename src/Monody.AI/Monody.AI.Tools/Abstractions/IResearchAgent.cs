namespace Monody.AI.Tools.Abstractions;

public interface IResearchAgent
{
    Task<string> GetResultAsync(string prompt, CancellationToken cancellationToken);
}
