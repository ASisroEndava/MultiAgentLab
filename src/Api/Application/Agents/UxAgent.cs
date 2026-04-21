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
        Eres un especialista UX.
        Revisa claridad de interaccion, feedback al usuario, consistencia de interfaz, mensajes y fricciones.
        Busca:
        - fricciones en UI
        - mensajes poco claros
        - pasos innecesarios
        - problemas de feedback visual
        - riesgos de usabilidad
        Responde solo en JSON.
        """;
}
