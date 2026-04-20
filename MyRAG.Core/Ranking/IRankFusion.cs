namespace MyRAG.Core.Ranking;

public interface IRankFusion
{
    /// <summary>
    /// Combines multiple lists of ranked items using a fusion algorithm.
    /// </summary>
    /// <typeparam name="T">The type of the item being ranked.</typeparam>
    /// <param name="rankedLists">A collection of ranked lists. Each list should be ordered by relevance (best first).</param>
    /// <param name="take">Number of items to return.</param>
    /// <param name="k">The constant used in the RRF formula (default is 60).</param>
    /// <returns>A list of items with their calculated fusion scores, ordered by score descending.</returns>
    IEnumerable<RankedItem<T>> Fuse<T>(IEnumerable<IEnumerable<T>> rankedLists, int? take = null, int k = 60) where T : notnull;
}
