using MyRAG.Core.Interfaces;
using MyRAG.Core.Models;

namespace MyRAG.Core.Ranking;

/// <summary>
/// Implementation of Reciprocal Rank Fusion (RRF) algorithm.
/// RRF is used to combine multiple search result lists into a single ranked list.
/// </summary>
public class ReciprocalRankFusion : IRankFusion
{
    /// <inheritdoc />
    public IEnumerable<RankedItem<T>> Fuse<T>(IEnumerable<IEnumerable<T>> rankedLists, int? take = null, int k = 60) where T : notnull
    {
        var scoreBoard = new Dictionary<T, double>();

        foreach (var list in rankedLists)
        {
            int rank = 1;
            foreach (var item in list)
            {
                // RRF formula: score = sum(1 / (k + rank))
                double score = 1.0 / (k + rank);
                
                if (scoreBoard.TryGetValue(item, out double currentScore))
                {
                    scoreBoard[item] = currentScore + score;
                }
                else
                {
                    scoreBoard[item] = score;
                }
                
                rank++;
            }
        }

        var results = scoreBoard
            .OrderByDescending(x => x.Value)
            .Select(x => new RankedItem<T>(x.Key, x.Value));

        if (take.HasValue)
        {
            results = results.Take(take.Value);
        }

        return results.ToList();
    }

    /// <summary>
    /// Legacy compatibility method to match the signature of the original tool.
    /// </summary>
    public List<T> CombineRRF<T>(List<T> vectorIds, List<T> keywordIds, int take = 5, int k = 60) where T : notnull
    {
        return Fuse(new[] { vectorIds, keywordIds }, take, k)
            .Select(x => x.Item)
            .ToList();
    }
}
