using System.Text.Json.Serialization;

namespace MultiAgentLab.Api.Domain;

public sealed class SkippedAgent
{
    [JsonPropertyName("agent")]
    public required string Agent { get; init; }

    [JsonPropertyName("reason")]
    public required string Reason { get; init; }
}
