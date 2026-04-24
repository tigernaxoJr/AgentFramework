using MyRAG.Core.Models;

namespace MyRAG.Core.Interfaces;

public interface IReranker
{
    Task<IEnumerable<RankedItem<Document>>> RerankAsync(string query, IEnumerable<Document> documents, CancellationToken cancellationToken = default);
}
