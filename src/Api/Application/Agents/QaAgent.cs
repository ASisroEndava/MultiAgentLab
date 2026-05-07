using MultiAgentLab.Api.Infrastructure.LLM;
using MultiAgentLab.Api.Infrastructure.Logging;

namespace MultiAgentLab.Api.Application.Agents;

public sealed class QaAgent : BaseReviewAgent
{
    public QaAgent(IModelRouter modelRouter, IExecutionLogger logger)
        : base(modelRouter, logger) { }

    public override string Name => "qa";

    protected override string SystemPrompt =>
        """
        You are a QA analyst specialized in testability.
        Review whether the story allows building acceptance criteria and test cases.
        Detect missing validations, edge scenarios, and undefined error states.
        Look for:
        - absence of Given/When/Then
        - undefined expected states
        - missing validations
        - edge scenario coverage
        Respond only in JSON.
        """;
}
