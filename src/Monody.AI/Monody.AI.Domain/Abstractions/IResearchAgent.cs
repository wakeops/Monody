using System.Threading;
using System.Threading.Tasks;

namespace Monody.AI.Domain.Abstractions;

public interface IResearchAgent
{
    Task<string> GetResultAsync(string prompt, CancellationToken cancellationToken);
}
