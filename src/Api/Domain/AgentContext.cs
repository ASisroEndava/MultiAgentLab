namespace MultiAgentLab.Api.Domain;

public sealed class AgentContext
{
    public required string ExecutionId { get; init; }
    public required string StoryId { get; init; }
    public required string Title { get; init; }
    public required string StoryText { get; init; }
    public required ProviderSelection Provider { get; init; }
    public required LoggingOptions Logging { get; init; }
    public Dictionary<string, object> SharedFacts { get; init; } = new();
}
