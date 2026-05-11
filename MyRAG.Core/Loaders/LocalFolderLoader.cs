using MyRAG.Core.Interfaces;
using MyRAG.Core.Models;
using System.Runtime.CompilerServices;

namespace MyRAG.Core.Loaders;

/// <summary>
/// 實作從本地資料夾載入文本檔案的讀取器。
/// 支援 .txt, .md, .csv 等純文字格式。
/// </summary>
public class LocalFolderLoader : IDocumentLoader
{
    private readonly string[] _supportedExtensions;

    /// <summary>
    /// 初始化 LocalFolderLoader。
    /// </summary>
    /// <param name="supportedExtensions">支援的副檔名清單 (例如：.txt, .md)。若為 null 則預設支援 .txt, .md, .json, .csv。</param>
    public LocalFolderLoader(string[]? supportedExtensions = null)
    {
        _supportedExtensions = supportedExtensions ?? new[] { ".txt", ".md", ".json", ".csv" };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<Document> LoadAsync(string source, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException($"找不到指定的資料夾路徑: {source}");
        }

        // 取得所有檔案
        var files = Directory.EnumerateFiles(source, "*.*", SearchOption.AllDirectories)
                             .Where(f => _supportedExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()));

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string content = await File.ReadAllTextAsync(file, cancellationToken);
            
            // 使用檔案完整路徑作為 ID 的基礎，確保唯一性
            // 也可以考慮使用檔案路徑的 SHA256 Hash
            string fileName = Path.GetFileName(file);
            
            yield return new Document
            {
                Id = fileName, // 預設使用檔名，使用者可根據需求調整為 FullPath
                Content = content,
                Source = file,
                Metadata = new Dictionary<string, object>
                {
                    { "file_name", fileName },
                    { "file_path", file },
                    { "extension", Path.GetExtension(file) },
                    { "last_modified", File.GetLastWriteTime(file) }
                }
            };
        }
    }
}
