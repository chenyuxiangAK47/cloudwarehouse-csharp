using CloudWarehouse.Backend.Services;
using Microsoft.Extensions.Configuration;

namespace CloudWarehouse.Tests;

public class QuoteAssistantTests
{
    private static string KbPath()
    {
        var fromRepo = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..",
            "CloudWarehouse.Backend", "KnowledgeBase"));
        if (Directory.Exists(fromRepo))
            return fromRepo;

        var fromOutput = Path.Combine(AppContext.BaseDirectory, "KnowledgeBase");
        return fromOutput;
    }

    private static QuoteAssistantService CreateService()
    {
        var loader = new KnowledgeBaseLoader(KbPath());
        var retriever = new KeywordRetriever(loader);
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Assistant:OpenAI:ApiKey"] = ""
        }).Build();
        return new QuoteAssistantService(retriever, loader, new StubHttpClientFactory(), config);
    }

    [Fact]
    public void Loader_SplitsMarkdown_IntoSections()
    {
        var chunks = KnowledgeBaseLoader.SplitMarkdown("demo.md", """
            # 标题A
            前言内容
            ## 小节一
            内容一
            ## 小节二
            内容二
            """).ToList();

        Assert.Equal(3, chunks.Count);
        Assert.Contains(chunks, c => c.Title.Contains("小节一") && c.Content.Contains("内容一"));
    }

    [Fact]
    public void Retriever_FindsHistoricalPricing_ForYunnanQuestion()
    {
        Assert.True(Directory.Exists(KbPath()), $"KnowledgeBase missing at {KbPath()}");
        var loader = new KnowledgeBaseLoader(KbPath());
        var retriever = new KeywordRetriever(loader);
        var hits = retriever.Retrieve("云南 0.3kg 应付为什么是1.50不是1.30 历史报价", 3);
        Assert.NotEmpty(hits);
        Assert.Contains(hits, h =>
            h.Chunk.Source.Contains("dual-track", StringComparison.OrdinalIgnoreCase)
            || h.Chunk.Content.Contains("1.50")
            || h.Chunk.Title.Contains("历史"));
    }

    [Fact]
    public async Task Ask_ReturnsGroundedExtractiveAnswer_WithCitations()
    {
        Assert.True(Directory.Exists(KbPath()), $"KnowledgeBase missing at {KbPath()}");
        var svc = CreateService();
        var result = await svc.AskAsync(new CloudWarehouse.Backend.Models.AssistantAskRequest
        {
            Question = "应收和应付双轨计价怎么取价？按发货日期吗？",
            TopK = 3
        });

        Assert.True(result.Grounded);
        Assert.Equal("kb-extractive", result.Mode);
        Assert.NotEmpty(result.Citations);
        Assert.Contains("双轨", result.Answer);
    }

    [Fact]
    public async Task Ask_EmptyQuestion_ReturnsValidationMode()
    {
        var svc = CreateService();
        var result = await svc.AskAsync(new CloudWarehouse.Backend.Models.AssistantAskRequest { Question = " " });
        Assert.Equal("validation", result.Mode);
        Assert.False(result.Grounded);
    }

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new();
    }
}
