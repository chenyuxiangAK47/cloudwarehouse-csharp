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
/// Lexical retrieval for built-in rule RAG (no vector DB / paid embeddings).
/// Chinese-friendly: unigrams + bigrams + Latin tokens; TF-IDF-ish + phrase/title boosts.
/// </summary>
public sealed class KeywordRetriever : IKeywordRetriever
{
    private static readonly Regex LatinWord = new(@"[A-Za-z0-9_\.\-]{2,}", RegexOptions.Compiled);
    private static readonly HashSet<string> Stopwords =
    [
        "的", "了", "吗", "呢", "是", "在", "和", "与", "或", "一个", "什么", "怎么", "如何",
        "请", "帮", "我", "一下", "这个", "那个", "可以", "能不能", "分别", "有", "哪些",
        "the", "a", "an", "is", "to", "of", "for", "and"
    ];

    /// <summary>Query-side synonym expansion so business phrases hit the right KB docs.</summary>
    private static readonly (string Needle, string[] Extra)[] Expansions =
    [
        ("双轨", ["应收", "应付", "客户报价", "成本价", "L列", "X列"]),
        ("应收", ["客户报价", "双轨", "报价表"]),
        ("应付", ["成本", "双轨", "成本价"]),
        ("历史价", ["发货日期", "生效", "EffectiveDate", "BillDate"]),
        ("取价", ["应收", "应付", "双轨", "历史"]),
        ("策略", ["Strategy", "计费", "Tier", "续重", "体积重"]),
        ("strategy", ["策略", "计费", "IBillingStrategy"]),
        ("导入", ["Excel", "预览", "顺序", "价表"]),
        ("取整", ["重量", "公斤", "续重", "区间"]),
    ];

    private readonly IKnowledgeBaseLoader _loader;
    private Dictionary<string, double>? _idf;
    private List<(KnowledgeChunk Chunk, Dictionary<string, double> Tf, string Haystack)>? _indexed;

    public KeywordRetriever(IKnowledgeBaseLoader loader)
    {
        _loader = loader;
    }

    public IReadOnlyList<(KnowledgeChunk Chunk, double Score)> Retrieve(string query, int topK)
    {
        EnsureIndex();
        var expanded = ExpandQuery(query);
        var qTokens = Tokenize(expanded);
        if (qTokens.Count == 0 || _indexed == null || _idf == null)
            return [];

        var qTf = ToTf(qTokens);
        var scored = new List<(KnowledgeChunk Chunk, double Score)>(_indexed.Count);

        foreach (var (chunk, docTf, haystack) in _indexed)
        {
            double score = 0;
            foreach (var (term, qWeight) in qTf)
            {
                if (!docTf.TryGetValue(term, out var tf))
                    continue;
                var idf = _idf.GetValueOrDefault(term, 0.5);
                score += qWeight * tf * idf;
            }

            // Title overlap boost (helps short FAQ questions)
            var titleTokens = Tokenize(chunk.Title);
            foreach (var t in titleTokens)
            {
                if (qTf.ContainsKey(t))
                    score += 0.35 * _idf.GetValueOrDefault(t, 0.5);
            }

            // Phrase containment boost for domain keywords present in query
            foreach (var phrase in DomainPhrasesIn(query))
            {
                if (haystack.Contains(phrase, StringComparison.OrdinalIgnoreCase))
                    score += 0.85;
            }

            if (score > 0)
                scored.Add((chunk, score));
        }

        return scored
            .OrderByDescending(x => x.Score)
            .Take(Math.Clamp(topK, 1, 8))
            .ToList();
    }

    private static string ExpandQuery(string query)
    {
        var sb = new StringBuilder(query);
        foreach (var (needle, extras) in Expansions)
        {
            if (query.Contains(needle, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var e in extras)
                    sb.Append(' ').Append(e);
            }
        }
        return sb.ToString();
    }

    private static IEnumerable<string> DomainPhrasesIn(string query)
    {
        string[] phrases =
        [
            "双轨", "应收", "应付", "历史价", "发货日", "重量取整", "Strategy", "策略模式",
            "导入顺序", "客户报价", "成本价", "体积重", "续重", "区间计费"
        ];
        foreach (var p in phrases)
        {
            if (query.Contains(p, StringComparison.OrdinalIgnoreCase))
                yield return p;
        }
    }

    private void EnsureIndex()
    {
        if (_indexed != null)
            return;

        var chunks = _loader.Load();
        var docs = new List<(KnowledgeChunk Chunk, Dictionary<string, double> Tf, string Haystack)>(chunks.Count);
        var df = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var chunk in chunks)
        {
            var haystack = $"{chunk.Title}\n{chunk.Content}";
            var tokens = Tokenize(haystack);
            var tf = ToTf(tokens);
            docs.Add((chunk, tf, haystack));
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
