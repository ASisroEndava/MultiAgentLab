namespace MultiAgentLab.Api.Domain;

public sealed class AgentSelection
{
    public required List<string> Invoked { get; init; }
    public required List<SkippedAgent> Skipped { get; init; }
}
