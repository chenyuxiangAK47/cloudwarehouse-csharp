using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using CloudWarehouse.Backend.Models;

namespace CloudWarehouse.Backend.Services;

public interface IQuoteAssistantService
{
    Task<AssistantAskResponse> AskAsync(AssistantAskRequest request, CancellationToken ct = default);
    IReadOnlyList<object> ListKnowledge();
}

/// <summary>
/// Built-in freight/quote rule knowledge retrieval (keyword / TF-IDF).
/// Optional LLM rewrite only when Assistant:OpenAI:ApiKey is set.
/// Never replaces FeeCalculationEngine as system of record.
/// </summary>
public sealed class QuoteAssistantService : IQuoteAssistantService
{
    public const double DefaultMinScore = 0.35;

    private readonly IKeywordRetriever _retriever;
    private readonly IKnowledgeBaseLoader _loader;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public QuoteAssistantService(
        IKeywordRetriever retriever,
        IKnowledgeBaseLoader loader,
        IHttpClientFactory httpClientFactory,
        IConfiguration config)
    {
        _retriever = retriever;
        _loader = loader;
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    public IReadOnlyList<object> ListKnowledge() =>
        _loader.Load()
            .GroupBy(c => c.Source)
            .Select(g => new
            {
                source = g.Key,
                title = g.First().Title.Split('·')[0].Trim(),
                sections = g.Count()
            })
            .Cast<object>()
            .ToList();

    public async Task<AssistantAskResponse> AskAsync(AssistantAskRequest request, CancellationToken ct = default)
    {
        var question = (request.Question ?? "").Trim();
        if (question.Length < 2)
        {
            return new AssistantAskResponse
            {
                Answer = "请输入具体问题，例如：「为什么云南 0.3kg 应付是 1.50 不是 1.30？」或「运单导入顺序是什么？」",
                Mode = "validation",
                Citations = [],
                Grounded = false
            };
        }

        var topK = request.TopK <= 0 ? 3 : request.TopK;
        var minScore = ParseMinScore();
        var hits = _retriever.Retrieve(question, topK)
            .Where(h => h.Score >= minScore)
            .ToList();

        var citations = hits.Select(h => new CitationDto
        {
            Source = h.Chunk.Source,
            Title = h.Chunk.Title,
            Score = Math.Round(h.Score, 4),
            Snippet = Truncate(h.Chunk.Content, 180)
        }).ToList();

        if (citations.Count == 0)
        {
            return new AssistantAskResponse
            {
                Answer = "未找到足够相关的规则片段（匹配分低于阈值）。请换关键词，或直接查阅导入/计价文档。" +
                         "可试：双轨计价、历史报价、重量取整、导入顺序、计费策略。" +
                         "说明：本工具仅检索内置规则说明，不参与运费结算计算。",
                Mode = "retrieval-miss",
                Citations = [],
                Grounded = false
            };
        }

        var apiKey = _config["Assistant:OpenAI:ApiKey"];
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                var llmAnswer = await GenerateWithLlmAsync(question, hits, apiKey, ct);
                if (!string.IsNullOrWhiteSpace(llmAnswer))
                {
                    return new AssistantAskResponse
                    {
                        Answer = llmAnswer.Trim() +
                                 "\n\n——\n说明：回答由检索片段 + 可选大模型改写生成，仅供查阅；正式金额以运单导入/计费引擎为准。",
                        Mode = "optional-llm",
                        Citations = citations,
                        Grounded = true
                    };
                }
            }
            catch
            {
                // Fall through to extractive mode
            }
        }

        return new AssistantAskResponse
        {
            Answer = BuildExtractiveAnswer(question, hits),
            Mode = "kb-extractive",
            Citations = citations,
            Grounded = true
        };
    }

    private double ParseMinScore()
    {
        var raw = _config["Assistant:MinScore"];
        if (double.TryParse(raw, out var v) && v >= 0)
            return v;
        return DefaultMinScore;
    }

    private static string BuildExtractiveAnswer(
        string question,
        IReadOnlyList<(KnowledgeChunk Chunk, double Score)> hits)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"针对「{question}」，从内置知识库检索到以下规则要点（本地关键词/TF-IDF，非向量语义搜索）：");
        sb.AppendLine();

        var i = 1;
        foreach (var (chunk, _) in hits)
        {
            sb.AppendLine($"【{i}. {chunk.Title}】（来源：{chunk.Source}）");
            sb.AppendLine(Truncate(chunk.Content, 420));
            sb.AppendLine();
            i++;
        }

        sb.Append("边界：仅供规则查阅；不修改 PriceRules，不改写账单金额。正式结算以 FeeCalculationEngine 为准。");
        return sb.ToString().Trim();
    }

    private async Task<string?> GenerateWithLlmAsync(
        string question,
        IReadOnlyList<(KnowledgeChunk Chunk, double Score)> hits,
        string apiKey,
        CancellationToken ct)
    {
        var endpoint = _config["Assistant:OpenAI:Endpoint"]
                       ?? "https://api.openai.com/v1/chat/completions";
        var model = _config["Assistant:OpenAI:Model"] ?? "gpt-4o-mini";

        var context = string.Join("\n\n---\n\n", hits.Select(h =>
            $"来源: {h.Chunk.Source}\n标题: {h.Chunk.Title}\n{h.Chunk.Content}"));

        var payload = new
        {
            model,
            temperature = 0.2,
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "你是云仓计价规则查阅助手。只根据提供的知识片段作答，使用简体中文。" +
                              "若片段不足请明确说明不确定。禁止编造价格数字；强调正式结算以系统计费引擎为准。"
                },
                new
                {
                    role = "user",
                    content = $"知识片段：\n{context}\n\n用户问题：{question}"
                }
            }
        };

        var client = _httpClientFactory.CreateClient("QuoteAssistantLlm");
        using var req = new HttpRequestMessage(HttpMethod.Post, endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        req.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var res = await client.SendAsync(req, ct);
        if (!res.IsSuccessStatusCode)
            return null;

        await using var stream = await res.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();
    }

    private static string Truncate(string text, int max)
    {
        var t = text.Replace("\r\n", "\n").Trim();
        if (t.Length <= max)
            return t;
        return t[..max].TrimEnd() + "…";
    }
}
