using MyRAG.Core.Interfaces;
using MyRAG.Core.Models;

namespace MyRAG.Core.Ranking;

/// <summary>
/// 實作 Reciprocal Rank Fusion (RRF) 演算法。
/// RRF 用於將多個搜尋結果列表合併為單一的排名列表。
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
    /// 舊版相容方法，用於匹配原始工具的簽章。
    /// </summary>
    public List<T> CombineRRF<T>(List<T> vectorIds, List<T> keywordIds, int take = 5, int k = 60) where T : notnull
    {
        return Fuse(new[] { vectorIds, keywordIds }, take, k)
            .Select(x => x.Item)
            .ToList();
    }
}
