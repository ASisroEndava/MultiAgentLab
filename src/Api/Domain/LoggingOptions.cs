using System.Text.Json.Serialization;

namespace MultiAgentLab.Api.Domain;

public sealed class LoggingOptions
{
    [JsonPropertyName("level")]
    public string Level { get; init; } = "standard";

    [JsonPropertyName("includePrompts")]
    public bool IncludePrompts { get; init; }

    [JsonPropertyName("includeResponses")]
    public bool IncludeResponses { get; init; }
}
