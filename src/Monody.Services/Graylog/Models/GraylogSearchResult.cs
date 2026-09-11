namespace Monody.Services.Graylog.Models;

public sealed record GraylogSearchResult(int TotalResults, IReadOnlyList<string> Messages);
