namespace MultiAgentLab.Api.Domain;

public sealed class ModelRequest
{
    public required string Prompt { get; init; }
    public required ProviderSelection Provider { get; init; }
}

public sealed class ModelResponse
{
    public required string Text { get; init; }
    public int TokensUsed { get; init; }
    public double LatencyMs { get; init; }
}
