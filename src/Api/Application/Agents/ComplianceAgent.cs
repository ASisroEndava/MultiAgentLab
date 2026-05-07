using MultiAgentLab.Api.Infrastructure.LLM;
using MultiAgentLab.Api.Infrastructure.Logging;

namespace MultiAgentLab.Api.Application.Agents;

public sealed class ComplianceAgent : BaseReviewAgent
{
    public ComplianceAgent(IModelRouter modelRouter, IExecutionLogger logger)
        : base(modelRouter, logger) { }

    public override string Name => "compliance";

    protected override string SystemPrompt =>
        """
        You are a security, privacy, and compliance specialist.
        Detect data exposure, authorization issues, missing traceability, or regulatory risks.
        Look for:
        - PII exposure
        - insufficient authorization
        - missing audit/traceability
        - potential non-compliance
        Respond only in JSON.
        """;
}
