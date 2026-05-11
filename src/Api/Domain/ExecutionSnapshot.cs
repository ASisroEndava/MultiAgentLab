using System.Text.Json.Serialization;

namespace MultiAgentLab.Api.Domain;

public sealed class ExecutionSnapshot
{
    [JsonPropertyName("executionId")]
    public required string ExecutionId { get; init; }

    [JsonPropertyName("timestamp")]
    public required string Timestamp { get; init; }

    [JsonPropertyName("storyId")]
    public required string StoryId { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("provider")]
    public required string Provider { get; init; }

    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("invokedAgents")]
    public List<string> InvokedAgents { get; init; } = [];
}
