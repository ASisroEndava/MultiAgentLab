using System.Text.Json.Serialization;

namespace MultiAgentLab.Api.Domain;

public sealed class ProviderSelection
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("model")]
    public required string Model { get; init; }

    [JsonPropertyName("region")]
    public string? Region { get; init; }

    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; init; }

    [JsonPropertyName("temperature")]
    public double Temperature { get; init; } = 0.2;

    [JsonPropertyName("maxTokens")]
    public int? MaxTokens { get; init; }
}
