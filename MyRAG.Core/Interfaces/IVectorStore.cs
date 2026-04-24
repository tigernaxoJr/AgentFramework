using MyRAG.Core.Models;

namespace MyRAG.Core.Interfaces;

public interface IVectorStore
{
    Task UpsertAsync(IEnumerable<Document> documents, CancellationToken cancellationToken = default);
    Task<IEnumerable<Document>> SearchAsync(string query, int topK = 5, CancellationToken cancellationToken = default);
    Task DeleteAsync(string documentId, CancellationToken cancellationToken = default);
}
