using MultiAgentLab.Api.Infrastructure.LLM;
using MultiAgentLab.Api.Infrastructure.Logging;

namespace MultiAgentLab.Api.Application.Agents;

public sealed class TechnicalAgent : BaseReviewAgent
{
    public TechnicalAgent(IModelRouter modelRouter, IExecutionLogger logger)
        : base(modelRouter, logger) { }

    public override string Name => "technical";

    protected override string SystemPrompt =>
        """
        You are a software architect/engineer specialized in technical impact.
        Detect technical risks, dependencies, asynchrony, consistency, duplicates, observability, and complexity.
        Look for:
        - technical risks
        - dependencies with other systems
        - need for asynchrony
        - idempotency
        - data consistency
        - observability
        Respond only in JSON.
        """;
}
