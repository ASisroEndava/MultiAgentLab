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
        Eres un revisor funcional especializado en historias de usuario.
        Analiza la historia y detecta ambiguedades, reglas faltantes, escenarios no definidos y preguntas necesarias.
        Busca:
        - ambiguedades
        - definiciones faltantes
        - reglas de negocio implicitas
        - comportamientos no definidos
        Responde exclusivamente en JSON.
        """;
}
