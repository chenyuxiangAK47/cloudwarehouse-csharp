using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Services;

public interface IKeywordRetriever
{
    IReadOnlyList<(KnowledgeChunk Chunk, double Score)> Retrieve(string query, int topK);
}

/// <summary>
/// Lightweight lexical retrieval (no vector DB / paid embeddings).
/// Chinese-friendly: unigrams + bigrams + Latin tokens; TF-IDF-ish scoring.
/// </summary>
public sealed class KeywordRetriever : IKeywordRetriever
{
    private static readonly Regex LatinWord = new(@"[A-Za-z0-9_\.\-]{2,}", RegexOptions.Compiled);
    private static readonly HashSet<string> Stopwords =
    [
        "的", "了", "吗", "呢", "是", "在", "和", "与", "或", "一个", "什么", "怎么", "如何",
        "请", "帮", "我", "一下", "这个", "那个", "可以", "能不能", "the", "a", "an", "is", "to", "of"
    ];

    private readonly IKnowledgeBaseLoader _loader;
    private Dictionary<string, double>? _idf;
    private List<(KnowledgeChunk Chunk, Dictionary<string, double> Tf)>? _indexed;

    public KeywordRetriever(IKnowledgeBaseLoader loader)
    {
        _loader = loader;
    }

    public IReadOnlyList<(KnowledgeChunk Chunk, double Score)> Retrieve(string query, int topK)
    {
        EnsureIndex();
        var qTokens = Tokenize(query);
        if (qTokens.Count == 0 || _indexed == null || _idf == null)
            return [];

        var qTf = ToTf(qTokens);
        var scored = new List<(KnowledgeChunk Chunk, double Score)>(_indexed.Count);

        foreach (var (chunk, docTf) in _indexed)
        {
            double score = 0;
            foreach (var (term, qWeight) in qTf)
            {
                if (!docTf.TryGetValue(term, out var tf))
                    continue;
                var idf = _idf.GetValueOrDefault(term, 0.5);
                score += qWeight * tf * idf;
            }

            if (score > 0)
                scored.Add((chunk, score));
        }

        return scored
            .OrderByDescending(x => x.Score)
            .Take(Math.Clamp(topK, 1, 8))
            .ToList();
    }

    private void EnsureIndex()
    {
        if (_indexed != null)
            return;

        var chunks = _loader.Load();
        var docs = new List<(KnowledgeChunk Chunk, Dictionary<string, double> Tf)>(chunks.Count);
        var df = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var chunk in chunks)
        {
            var tokens = Tokenize($"{chunk.Title}\n{chunk.Content}");
            var tf = ToTf(tokens);
            docs.Add((chunk, tf));
            foreach (var term in tf.Keys)
                df[term] = df.GetValueOrDefault(term) + 1;
        }

        var n = Math.Max(docs.Count, 1);
        _idf = df.ToDictionary(
            kv => kv.Key,
            kv => Math.Log((n + 1.0) / (kv.Value + 1.0)) + 1.0,
            StringComparer.OrdinalIgnoreCase);
        _indexed = docs;
    }

    internal static List<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return [];

        var tokens = new List<string>();
        foreach (Match m in LatinWord.Matches(text))
        {
            var w = m.Value.ToLowerInvariant();
            if (!Stopwords.Contains(w))
                tokens.Add(w);
        }

        var sb = new StringBuilder();
        foreach (var ch in text.Normalize(NormalizationForm.FormC))
        {
            if (IsCjk(ch))
                sb.Append(ch);
            else if (sb.Length > 0)
            {
                AddCjkGrams(sb.ToString(), tokens);
                sb.Clear();
            }
        }

        if (sb.Length > 0)
            AddCjkGrams(sb.ToString(), tokens);

        return tokens;
    }

    private static void AddCjkGrams(string run, List<string> tokens)
    {
        for (var i = 0; i < run.Length; i++)
        {
            var uni = run[i].ToString();
            if (!Stopwords.Contains(uni))
                tokens.Add(uni);
            if (i + 1 < run.Length)
                tokens.Add(run.Substring(i, 2));
        }
    }

    private static Dictionary<string, double> ToTf(IReadOnlyList<string> tokens)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in tokens)
            counts[t] = counts.GetValueOrDefault(t) + 1;

        var max = Math.Max(counts.Values.DefaultIfEmpty(1).Max(), 1);
        return counts.ToDictionary(
            kv => kv.Key,
            kv => 0.5 + 0.5 * (kv.Value / (double)max),
            StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsCjk(char ch) =>
        char.GetUnicodeCategory(ch) is UnicodeCategory.OtherLetter
        && ch is >= '\u4E00' and <= '\u9FFF';
}
