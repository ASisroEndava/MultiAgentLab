using System.Text.Json.Serialization;

namespace MultiAgentLab.Api.Domain;

public sealed class ComparisonResult
{
    [JsonPropertyName("storyId")]
    public required string StoryId { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("snapshotA")]
    public required ExecutionSnapshot SnapshotA { get; init; }

    [JsonPropertyName("snapshotB")]
    public required ExecutionSnapshot SnapshotB { get; init; }

    [JsonPropertyName("issuesOnlyInA")]
    public List<string> IssuesOnlyInA { get; init; } = [];

    [JsonPropertyName("issuesOnlyInB")]
    public List<string> IssuesOnlyInB { get; init; } = [];

    [JsonPropertyName("issuesInBoth")]
    public List<string> IssuesInBoth { get; init; } = [];

    [JsonPropertyName("recommendationsOnlyInA")]
    public List<string> RecommendationsOnlyInA { get; init; } = [];

    [JsonPropertyName("recommendationsOnlyInB")]
    public List<string> RecommendationsOnlyInB { get; init; } = [];

    [JsonPropertyName("recommendationsInBoth")]
    public List<string> RecommendationsInBoth { get; init; } = [];

    [JsonPropertyName("agentsOnlyInA")]
    public List<string> AgentsOnlyInA { get; init; } = [];

    [JsonPropertyName("agentsOnlyInB")]
    public List<string> AgentsOnlyInB { get; init; } = [];

    [JsonPropertyName("agentsInBoth")]
    public List<string> AgentsInBoth { get; init; } = [];
}
