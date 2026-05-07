using MultiAgentLab.Api.Infrastructure.LLM;
using MultiAgentLab.Api.Infrastructure.Logging;

namespace MultiAgentLab.Api.Application.Agents;

public sealed class ClarityAgent : BaseReviewAgent
{
    public ClarityAgent(IModelRouter modelRouter, IExecutionLogger logger)
        : base(modelRouter, logger) { }

    public override string Name => "clarity";

    protected override string SystemPrompt =>
        """
        You are a functional reviewer specialized in user stories.
        Analyze the story and detect ambiguities, missing rules, undefined scenarios, and necessary questions.
        Look for:
        - ambiguities
        - missing definitions
        - implicit business rules
        - undefined behaviors
        Respond exclusively in JSON.
        """;
}
