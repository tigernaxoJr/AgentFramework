namespace MyRAG.Core.Models;

public record RankedItem<T>(T Item, double Score) where T : notnull;
