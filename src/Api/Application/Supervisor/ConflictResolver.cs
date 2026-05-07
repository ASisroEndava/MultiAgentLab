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
                .Any(r => ContainsAny(r, "simple", "immediate", "quick", "direct", "inline"));
            var techWantsRestriction = techResult.Issues
                .Any(i => ContainsAny(i, "impact", "restriction", "state", "process", "consistency"));

            if (uxWantsSimplicity && techWantsRestriction)
            {
                conflicts.Add("UX proposes simplified interaction; technical detects restrictions due to state or consistency");
                resolutions.Add("Prioritize technical feasibility; apply visible restrictions in UI");
            }
        }

        if (uxResult != null && complianceResult != null)
        {
            var uxSimplifies = uxResult.Recommendations
                .Any(r => ContainsAny(r, "simplify", "omit", "reduce", "fewer steps"));
            var complianceRequires = complianceResult.Issues
                .Any(i => ContainsAny(i, "validate", "authorization", "identity", "audit", "encryption"));

            if (uxSimplifies && complianceRequires)
            {
                conflicts.Add("UX proposes simplification that may compromise security or privacy");
                resolutions.Add("Compliance takes priority; maintain security validations even if they add friction");
            }
        }

        if (qaResult != null)
        {
            var severeMissing = qaResult.Issues.Count >= 3;
            if (severeMissing)
            {
                var hasTestabilityGap = qaResult.Issues
                    .Any(i => ContainsAny(i, "criteria", "acceptance", "scenario", "validation", "error"));
                if (hasTestabilityGap)
                {
                    conflicts.Add("QA detects severe absence of acceptance criteria");
                    resolutions.Add("Final status should not be green while basic acceptance criteria are missing");
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
