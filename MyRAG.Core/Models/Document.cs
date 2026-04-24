namespace MyRAG.Core.Models;

public class Document
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Content { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
    public string? Source { get; set; }
}
