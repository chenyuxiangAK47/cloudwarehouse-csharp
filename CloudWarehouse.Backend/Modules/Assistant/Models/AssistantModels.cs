namespace CloudWarehouse.Backend.Models;

public sealed class KnowledgeChunk
{
    public required string Id { get; init; }
    public required string Source { get; init; }
    public required string Title { get; init; }
    public required string Content { get; init; }
}

public sealed class CitationDto
{
    public required string Source { get; init; }
    public required string Title { get; init; }
    public double Score { get; init; }
    public required string Snippet { get; init; }
}

public sealed class AssistantAskRequest
{
    public string Question { get; set; } = "";
    public int TopK { get; set; } = 3;
}

public sealed class AssistantAskResponse
{
    public required string Answer { get; init; }
    public required string Mode { get; init; }
    public required IReadOnlyList<CitationDto> Citations { get; init; }
    public bool Grounded { get; init; }
}
