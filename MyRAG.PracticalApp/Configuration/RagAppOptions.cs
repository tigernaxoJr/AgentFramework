namespace MyRAG.PracticalApp.Configuration;

public class RagAppOptions
{
    public VectorDbOptions VectorDb { get; set; } = new();
    public QueryExpansionOptions QueryExpansion { get; set; } = new();
    public ChunkingOptions Chunking { get; set; } = new();
    public OnnxEmbeddingOptions OnnxEmbedding { get; set; } = new();
    public OnnxRerankerOptions OnnxReranker { get; set; } = new();
}

public class VectorDbOptions
{
    public string Provider { get; set; } = "LanceDB";
    public LanceDbOptions LanceDB { get; set; } = new();
    public SqlServerOptions SqlServer { get; set; } = new();
}

public class LanceDbOptions
{
    public string Path { get; set; } = "./lancedb_data";
    public string TableName { get; set; } = "documents";
}

public class SqlServerOptions
{
    public string ConnectionString { get; set; } = "";
    public string TableName { get; set; } = "RagDocuments";
    public int Dimensions { get; set; } = 1024;
}

public class QueryExpansionOptions
{
    public bool Enabled { get; set; } = false;
    public string ModelId { get; set; } = "";
    public string Endpoint { get; set; } = "";
    public string ApiKey { get; set; } = "dummy-key";
}

public class ChunkingOptions
{
    public string Strategy { get; set; } = "Batched";
}

public class OnnxEmbeddingOptions
{
    public string ModelPath { get; set; } = "";
    public string TokenizerPath { get; set; } = "";
    public bool UseGPU { get; set; } = false;
}

public class OnnxRerankerOptions
{
    public bool Enabled { get; set; } = false;
    public string ModelPath { get; set; } = "";
    public string TokenizerPath { get; set; } = "";
    public bool UseGPU { get; set; } = false;
}
