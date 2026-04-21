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
        Eres un especialista en seguridad, privacidad y compliance.
        Detecta exposicion de datos, problemas de autorizacion, trazabilidad faltante o riesgos regulatorios.
        Busca:
        - exposicion de PII
        - autorizacion insuficiente
        - auditoria/trazabilidad faltante
        - incumplimientos potenciales
        Responde solo en JSON.
        """;
}
