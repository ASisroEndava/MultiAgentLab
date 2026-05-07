using MultiAgentLab.Api.Infrastructure.LLM;
using MultiAgentLab.Api.Infrastructure.Logging;

namespace MultiAgentLab.Api.Application.Agents;

public sealed class UxAgent : BaseReviewAgent
{
    public UxAgent(IModelRouter modelRouter, IExecutionLogger logger)
        : base(modelRouter, logger) { }

    public override string Name => "ux";

    protected override string SystemPrompt =>
        """
        You are a UX specialist.
        Review interaction clarity, user feedback, interface consistency, messages, and friction points.
        Look for:
        - UI friction
        - unclear messages
        - unnecessary steps
        - visual feedback issues
        - usability risks
        Respond only in JSON.
        """;
}
