using System.Text.Json.Serialization;

namespace MultiAgentLab.Api.Domain;

public sealed class ExecutionLogEvent
{
    [JsonPropertyName("executionId")]
    public required string ExecutionId { get; init; }

    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }

    [JsonPropertyName("eventType")]
    public required string EventType { get; init; }

    [JsonPropertyName("data")]
    public required object Data { get; init; }
}
