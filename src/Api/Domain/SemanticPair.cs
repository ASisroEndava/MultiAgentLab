using System.Text.Json.Serialization;

namespace MultiAgentLab.Api.Domain;

public sealed class SemanticPair
{
    [JsonPropertyName("a")]
    public required string A { get; init; }

    [JsonPropertyName("b")]
    public required string B { get; init; }
}

public sealed class SemanticDiff
{
    [JsonPropertyName("similar")]
    public List<SemanticPair> Similar { get; init; } = [];

    [JsonPropertyName("onlyInA")]
    public List<string> OnlyInA { get; init; } = [];

    [JsonPropertyName("onlyInB")]
    public List<string> OnlyInB { get; init; } = [];
}

public sealed class SemanticComparisonResult
{
    [JsonPropertyName("storyId")]
    public required string StoryId { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("snapshotA")]
    public required ExecutionSnapshot SnapshotA { get; init; }

    [JsonPropertyName("snapshotB")]
    public required ExecutionSnapshot SnapshotB { get; init; }

    [JsonPropertyName("issues")]
    public required SemanticDiff Issues { get; init; }

    [JsonPropertyName("recommendations")]
    public required SemanticDiff Recommendations { get; init; }

    [JsonPropertyName("agentsOnlyInA")]
    public List<string> AgentsOnlyInA { get; init; } = [];

    [JsonPropertyName("agentsOnlyInB")]
    public List<string> AgentsOnlyInB { get; init; } = [];

    [JsonPropertyName("agentsInBoth")]
    public List<string> AgentsInBoth { get; init; } = [];
}

public sealed class SemanticCompareRequest
{
    [JsonPropertyName("a")]
    public required string A { get; init; }

    [JsonPropertyName("b")]
    public required string B { get; init; }

    [JsonPropertyName("provider")]
    public required ProviderSelection Provider { get; init; }
}
