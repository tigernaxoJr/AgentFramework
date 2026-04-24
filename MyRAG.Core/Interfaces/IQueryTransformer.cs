namespace MyRAG.Core.Interfaces;

/// <summary>
/// Defines a service that transforms a user query into a higher-quality retrieval query.
/// </summary>
public interface IQueryTransformer
{
    /// <summary>
    /// Transforms the input query.
    /// </summary>
    /// <param name="query">The original user query.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The transformed and optimized query.</returns>
    Task<string> TransformAsync(string query, CancellationToken cancellationToken = default);
}
