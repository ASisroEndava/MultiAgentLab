using System.Text.Json.Serialization;

namespace MultiAgentLab.Api.Domain;

public sealed class ReviewRequest
{
    [JsonPropertyName("storyId")]
    public required string StoryId { get; init; }

    [JsonPropertyName("title")]
    public required string Title { get; init; }

    [JsonPropertyName("storyText")]
    public required string StoryText { get; init; }

    [JsonPropertyName("provider")]
    public required ProviderSelection Provider { get; init; }

    [JsonPropertyName("logging")]
    public LoggingOptions Logging { get; init; } = new();
}
