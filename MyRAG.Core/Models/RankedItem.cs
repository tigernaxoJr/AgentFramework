namespace MyRAG.Core.Models;

/// <summary>
/// 代表一個經過排名的項目及其對應的分數。
/// </summary>
/// <typeparam name="T">被排名的項目型別。</typeparam>
/// <param name="Item">實際的項目。</param>
/// <param name="Score">與查詢相關性的排名分數 (分數越高通常代表越相關)。</param>
public record RankedItem<T>(T Item, double Score) where T : notnull;
