using MultiAgentLab.Api.Domain;

namespace MultiAgentLab.Api.Application.Supervisor;

public sealed class AgentSelectionRules
{
    private static readonly string[] UxKeywords =
    {
        "pantalla", "boton", "formulario", "perfil", "mostrar", "interfaz",
        "ui", "ux", "pagina", "click", "navegacion", "feedback", "mensaje",
        "copy", "label", "texto", "login", "menu", "modal", "popup",
        "confirmar", "cancelar", "editar", "visualizar"
    };

    private static readonly string[] TechnicalKeywords =
    {
        "retry", "reintento", "reintentar", "scheduler", "cola", "queue",
        "integracion", "notificacion", "persistencia", "backend", "api",
        "asincrono", "async", "batch", "cron", "webhook", "microservicio",
        "base de datos", "cache", "consistencia", "idempotencia", "timeout",
        "performance", "escalabilidad"
    };

    private static readonly string[] ComplianceKeywords =
    {
        "datos personales", "pii", "documento", "reporte", "transacciones",
        "auditoria", "regulacion", "compliance", "seguridad", "privacidad",
        "autorizacion", "descarga", "exportacion", "gdpr", "sensible",
        "cifrado", "encriptacion", "fraude", "titular"
    };

    private static readonly string[] QaKeywords =
    {
        "validacion", "error", "estado", "regla", "criterio", "aceptacion",
        "flujo", "escenario", "limite", "maximo", "minimo", "obligatorio",
        "requerido", "falla", "exito", "intentos", "expiracion"
    };

    private static readonly string[] TrivialKeywords =
    {
        "cambiar texto", "renombrar", "ajustar copy", "cambiar label",
        "modificar etiqueta", "actualizar nombre"
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
                Reason = "Cambio trivial sin reglas de validacion ni flujos complejos"
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
                Reason = "No se detecto impacto tecnico, integraciones ni procesos asincronos"
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
                Reason = "No se detecto interaccion de usuario visible ni elementos de interfaz"
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
                Reason = "No se detectaron datos sensibles ni requisitos regulatorios"
            });
        }

        return new AgentSelection
        {
            Invoked = invoked,
            Skipped = skipped
        };
    }
}
