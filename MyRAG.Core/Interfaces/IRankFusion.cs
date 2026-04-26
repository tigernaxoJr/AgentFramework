using MyRAG.Core.Models;

namespace MyRAG.Core.Interfaces;

/// <summary>
/// 定義排名融合服務，用於將多個檢索結果合併並重新排名。
/// </summary>
public interface IRankFusion
{
    /// <summary>
    /// 使用融合演算法合併多個排名列表。
    /// </summary>
    /// <typeparam name="T">被排名的項目型別。</typeparam>
    /// <param name="rankedLists">排名列表的集合。每個列表應依相關性排序 (最好的在前面)。</param>
    /// <param name="take">要回傳的項目數量 (若未指定則回傳全部)。</param>
    /// <param name="k">RRF (Reciprocal Rank Fusion) 公式中使用的常數 (預設為 60)。</param>
    /// <returns>包含計算後融合分數的項目列表，依分數遞減排序。</returns>
    IEnumerable<RankedItem<T>> Fuse<T>(IEnumerable<IEnumerable<T>> rankedLists, int? take = null, int k = 60) where T : notnull;
}
