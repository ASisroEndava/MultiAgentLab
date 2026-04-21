using MultiAgentLab.Api.Domain;

namespace MultiAgentLab.Api.Application.Agents;

public interface IReviewAgent
{
    string Name { get; }

    Task<AgentResult> ExecuteAsync(
        AgentContext context,
        CancellationToken cancellationToken = default);
}
