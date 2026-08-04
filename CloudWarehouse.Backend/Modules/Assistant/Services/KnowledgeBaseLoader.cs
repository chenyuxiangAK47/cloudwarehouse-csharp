using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Services;

public interface IKnowledgeBaseLoader
{
    IReadOnlyList<KnowledgeChunk> Load();
}

/// <summary>
/// Loads markdown knowledge files and splits them into retrievable chunks (by ## headings).
/// </summary>
public sealed class KnowledgeBaseLoader : IKnowledgeBaseLoader
{
    private readonly string _root;
    private IReadOnlyList<KnowledgeChunk>? _cache;

    public KnowledgeBaseLoader(IWebHostEnvironment env, IConfiguration config)
    {
        var configured = config["Assistant:KnowledgeBasePath"];
        if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
        {
            _root = Path.GetFullPath(configured);
            return;
        }

        var contentRoot = Path.Combine(env.ContentRootPath, "KnowledgeBase");
        if (Directory.Exists(contentRoot))
        {
            _root = contentRoot;
            return;
        }

        _root = Path.Combine(AppContext.BaseDirectory, "KnowledgeBase");
    }

    public KnowledgeBaseLoader(string knowledgeBaseRoot)
    {
        _root = Path.GetFullPath(knowledgeBaseRoot);
    }

    public IReadOnlyList<KnowledgeChunk> Load()
    {
        if (_cache != null)
            return _cache;

        if (!Directory.Exists(_root))
        {
            _cache = [];
            return _cache;
        }

        var chunks = new List<KnowledgeChunk>();
        foreach (var file in Directory.EnumerateFiles(_root, "*.md", SearchOption.TopDirectoryOnly)
                     .OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
        {
            var source = Path.GetFileName(file);
            var text = File.ReadAllText(file);
            chunks.AddRange(SplitMarkdown(source, text));
        }

        _cache = chunks;
        return _cache;
    }

    public static IEnumerable<KnowledgeChunk> SplitMarkdown(string source, string text)
    {
        var lines = text.Replace("\r\n", "\n").Split('\n');
        string? docTitle = null;
        var sectionTitle = "概述";
        var body = new List<string>();
        var sectionIndex = 0;
        var chunks = new List<KnowledgeChunk>();

        void Flush()
        {
            var content = string.Join("\n", body).Trim();
            if (string.IsNullOrWhiteSpace(content))
                return;

            var title = docTitle == null ? sectionTitle : $"{docTitle} · {sectionTitle}";
            chunks.Add(new KnowledgeChunk
            {
                Id = $"{source}#{sectionIndex}",
                Source = source,
                Title = title,
                Content = content
            });
            sectionIndex++;
            body.Clear();
        }

        foreach (var raw in lines)
        {
            var line = raw.TrimEnd();
            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                Flush();
                docTitle = line[2..].Trim();
                sectionTitle = "概述";
                continue;
            }

            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                Flush();
                sectionTitle = line[3..].Trim();
                continue;
            }

            body.Add(line);
        }

        Flush();
        return chunks;
    }
}
