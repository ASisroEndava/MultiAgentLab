using MultiAgentLab.Api.Domain;

namespace MultiAgentLab.Api.Application.Supervisor;

public sealed class AgentSelectionRules
{
    private static readonly string[] UxKeywords =
    {
        "screen", "button", "form", "profile", "display", "interface",
        "ui", "ux", "page", "click", "navigation", "feedback", "message",
        "copy", "label", "text", "login", "menu", "modal", "popup",
        "confirm", "cancel", "edit", "view"
    };

    private static readonly string[] TechnicalKeywords =
    {
        "retry", "retries", "scheduler", "queue",
        "integration", "notification", "persistence", "backend", "api",
        "asynchronous", "async", "batch", "cron", "webhook", "microservice",
        "database", "cache", "consistency", "idempotency", "timeout",
        "performance", "scalability"
    };

    private static readonly string[] ComplianceKeywords =
    {
        "personal data", "pii", "document", "report", "transactions",
        "audit", "regulation", "compliance", "security", "privacy",
        "authorization", "download", "export", "gdpr", "sensitive",
        "encryption", "fraud", "holder", "identity"
    };

    private static readonly string[] QaKeywords =
    {
        "validation", "error", "state", "rule", "criteria", "acceptance",
        "flow", "scenario", "limit", "maximum", "minimum", "mandatory",
        "required", "failure", "success", "attempts", "expiration"
    };

    private static readonly string[] TrivialKeywords =
    {
        "change text", "rename", "adjust copy", "change label",
        "update name", "modify tag"
    };

    public AgentSelection Select(ReviewRequest request)
    {
        var text = $"{request.Title} {request.StoryText}".ToLowerInvariant();
        var invoked = new List<string>();
        var skipped = new List<SkippedAgent>();

        var isTrivial = TrivialKeywords.Any(k => text.Contains(k));
        var hasUxSignals = UxKeywords.Any(k => text.Contains(k));
        var hasTechSignals = TechnicalKeywords.Any(k => text.Contains(k));
        var hasComplianceSignals = ComplianceKeywords.Any(k => text.Contains(k));
        var hasQaSignals = QaKeywords.Any(k => text.Contains(k));

        if (!isTrivial)
        {
            invoked.Add("clarity");
        }
        else
        {
            invoked.Add("clarity");
        }

        if (hasQaSignals || hasTechSignals || hasComplianceSignals || !isTrivial)
        {
            invoked.Add("qa");
        }
        else
        {
            skipped.Add(new SkippedAgent
            {
                Agent = "qa",
                Reason = "Trivial change without validation rules or complex flows"
            });
        }

        if (hasTechSignals)
        {
            invoked.Add("technical");
        }
        else
        {
            skipped.Add(new SkippedAgent
            {
                Agent = "technical",
                Reason = "No technical impact, integrations, or asynchronous processes detected"
            });
        }

        if (hasUxSignals)
        {
            invoked.Add("ux");
        }
        else
        {
            skipped.Add(new SkippedAgent
            {
                Agent = "ux",
                Reason = "No visible user interaction or interface elements detected"
            });
        }

        if (hasComplianceSignals)
        {
            invoked.Add("compliance");
        }
        else
        {
            skipped.Add(new SkippedAgent
            {
                Agent = "compliance",
                Reason = "No sensitive data or regulatory requirements detected"
            });
        }

        return new AgentSelection
        {
            Invoked = invoked,
            Skipped = skipped
        };
    }
}
