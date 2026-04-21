using System.Text.Json.Serialization;

namespace MultiAgentLab.Api.Domain;

public sealed class AgentResult
{
    [JsonPropertyName("agent")]
    public required string Agent { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("score")]
    public double Score { get; init; }

    [JsonPropertyName("issues")]
    public List<string> Issues { get; init; } = new();

    [JsonPropertyName("recommendations")]
    public List<string> Recommendations { get; init; } = new();

    [JsonPropertyName("questions")]
    public List<string> Questions { get; init; } = new();

    [JsonPropertyName("rawSummary")]
    public string? RawSummary { get; init; }
}
