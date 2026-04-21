using MultiAgentLab.Api.Domain;

namespace MultiAgentLab.Api.Application.Supervisor;

public sealed class ConflictResolver
{
    public (List<string> Conflicts, List<string> Resolutions) Detect(List<AgentResult> results)
    {
        var conflicts = new List<string>();
        var resolutions = new List<string>();

        var uxResult = results.FirstOrDefault(r => r.Agent == "ux");
        var techResult = results.FirstOrDefault(r => r.Agent == "technical");
        var complianceResult = results.FirstOrDefault(r => r.Agent == "compliance");
        var qaResult = results.FirstOrDefault(r => r.Agent == "qa");

        if (uxResult != null && techResult != null)
        {
            var uxWantsSimplicity = uxResult.Recommendations
                .Any(r => ContainsAny(r, "simple", "inmediata", "rapida", "directa", "inline"));
            var techWantsRestriction = techResult.Issues
                .Any(i => ContainsAny(i, "impacto", "restriccion", "estado", "proceso", "consistencia"));

            if (uxWantsSimplicity && techWantsRestriction)
            {
                conflicts.Add("UX propone interaccion simplificada; tecnico detecta restricciones por estado o consistencia");
                resolutions.Add("Priorizar factibilidad tecnica; aplicar restricciones visibles en UI");
            }
        }

        if (uxResult != null && complianceResult != null)
        {
            var uxSimplifies = uxResult.Recommendations
                .Any(r => ContainsAny(r, "simplificar", "omitir", "reducir", "menos pasos"));
            var complianceRequires = complianceResult.Issues
                .Any(i => ContainsAny(i, "validar", "autorizacion", "identidad", "auditoria", "cifrado"));

            if (uxSimplifies && complianceRequires)
            {
                conflicts.Add("UX propone simplificacion que puede comprometer seguridad o privacidad");
                resolutions.Add("Compliance tiene prioridad; mantener validaciones de seguridad aunque agreguen friccion");
            }
        }

        if (qaResult != null)
        {
            var severeMissing = qaResult.Issues.Count >= 3;
            if (severeMissing)
            {
                var hasTestabilityGap = qaResult.Issues
                    .Any(i => ContainsAny(i, "criterio", "aceptacion", "escenario", "validacion", "error"));
                if (hasTestabilityGap)
                {
                    conflicts.Add("QA detecta ausencia severa de criterios de aceptacion");
                    resolutions.Add("El estado final no debe ser verde mientras falten criterios de aceptacion basicos");
                }
            }
        }

        return (conflicts, resolutions);
    }

    private static bool ContainsAny(string text, params string[] keywords)
    {
        var lower = text.ToLowerInvariant();
        return keywords.Any(k => lower.Contains(k));
    }
}
