using CloudWarehouse.Backend.Models;
using CloudWarehouse.Backend.Services;
using Microsoft.Extensions.Configuration;

namespace CloudWarehouse.Tests;

/// <summary>
/// Lightweight eval set for the built-in rule knowledge base (not a full IR benchmark).
/// Measures: top-1 expected source hit rate on 15 fixed questions.
/// </summary>
public class QuoteAssistantEvalTests
{
    private static string KbPath()
    {
        var fromRepo = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "CloudWarehouse.Backend", "KnowledgeBase"));
        if (Directory.Exists(fromRepo))
            return fromRepo;
        return Path.Combine(AppContext.BaseDirectory, "KnowledgeBase");
    }

    private static readonly (string Question, string ExpectedSourceSubstring)[] GoldenSet =
    [
        ("为什么云南0.3kg应付是1.50不是最新的1.30？", "dual-track"),
        ("应收和应付双轨计价分别怎么取价？", "dual-track"),
        ("按发货日期取历史价是什么意思？", "dual-track"),
        ("利润怎么算？应收减应付吗？", "dual-track"),
        ("重量取整规则是什么？", "weight-rounding"),
        ("超过5kg怎么计费？", "weight-rounding"),
        ("0.3kg和0.5kg取整怎么处理？", "weight-rounding"),
        ("区间计费和续重计费有什么区别？", "weight-rounding"),
        ("93演示正确的Excel导入顺序是什么？", "import-faq"),
        ("把运单上传到客户报价导入会怎样？", "import-faq"),
        ("目的省为空的退件费行为什么失败？", "import-faq"),
        ("站点匹配规则是什么？", "import-faq"),
        ("计费策略模式Strategy Pattern怎么设计的？", "billing-strategy"),
        ("怎么扩展新的计费类型？", "billing-strategy"),
        ("RAG助手会不会替代结算引擎？", "billing-strategy"),
    ];

    [Fact]
    public void GoldenSet_Top1SourceHitRate_AtLeast80Percent()
    {
        Assert.True(Directory.Exists(KbPath()), $"KnowledgeBase missing at {KbPath()}");
        var loader = new KnowledgeBaseLoader(KbPath());
        var retriever = new KeywordRetriever(loader);

        var hits = 0;
        var misses = new List<string>();
        foreach (var (q, expected) in GoldenSet)
        {
            var top = retriever.Retrieve(q, 1).FirstOrDefault();
            if (top.Chunk != null &&
                top.Chunk.Source.Contains(expected, StringComparison.OrdinalIgnoreCase) &&
                top.Score >= QuoteAssistantService.DefaultMinScore)
            {
                hits++;
            }
            else
            {
                var got = top.Chunk?.Source ?? "(none)";
                misses.Add($"{q} => {got} (score={top.Score:0.####})");
            }
        }

        var rate = hits / (double)GoldenSet.Length;
        Assert.True(rate >= 0.80,
            $"Top-1 source hit rate {rate:P0} ({hits}/{GoldenSet.Length}). Misses:\n" +
            string.Join("\n", misses));
    }

    [Fact]
    public async Task Ask_LowRelevance_ReturnsRetrievalMiss()
    {
        var loader = new KnowledgeBaseLoader(KbPath());
        var retriever = new KeywordRetriever(loader);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Assistant:OpenAI:ApiKey"] = "",
            ["Assistant:MinScore"] = "0.28"
        }).Build();
        var svc = new QuoteAssistantService(retriever, loader, new StubHttp(), config);

        var result = await svc.AskAsync(new AssistantAskRequest
        {
            // Intentionally irrelevant tokens with no overlap to freight KB
            Question = "xyzzy quux foobarbaz quantum broccoli playlist"
        });

        Assert.Equal("retrieval-miss", result.Mode);
        Assert.False(result.Grounded);
        Assert.Empty(result.Citations);
    }

    private sealed class StubHttp : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
