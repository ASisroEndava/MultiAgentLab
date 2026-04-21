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
        Eres un analista QA especializado en testabilidad.
        Revisa si la historia permite construir criterios de aceptacion y casos de prueba.
        Detecta validaciones faltantes, escenarios borde y estados de error no definidos.
        Busca:
        - ausencia de Given/When/Then
        - estados esperados no definidos
        - validaciones faltantes
        - cobertura de escenarios borde
        Responde solo en JSON.
        """;
}
