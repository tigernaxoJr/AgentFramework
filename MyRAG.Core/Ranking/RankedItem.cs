namespace MyRAG.Core.Ranking;

public record RankedItem<T>(T Item, double Score) where T : notnull;
