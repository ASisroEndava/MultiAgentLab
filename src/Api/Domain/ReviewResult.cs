using System.Text.Json.Serialization;

namespace MultiAgentLab.Api.Domain;

public sealed class ReviewResult
{
    [JsonPropertyName("executionId")]
    public required string ExecutionId { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("provider")]
    public required string Provider { get; init; }

    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("invokedAgents")]
    public List<string> InvokedAgents { get; init; } = new();

    [JsonPropertyName("skippedAgents")]
    public List<SkippedAgent> SkippedAgents { get; init; } = new();

    [JsonPropertyName("issues")]
    public List<string> Issues { get; init; } = new();

    [JsonPropertyName("recommendations")]
    public List<string> Recommendations { get; init; } = new();

    [JsonPropertyName("conflicts")]
    public List<string> Conflicts { get; init; } = new();

    [JsonPropertyName("resolution")]
    public List<string> Resolution { get; init; } = new();

    [JsonPropertyName("agentResults")]
    public List<AgentResult> AgentResults { get; init; } = new();
}
