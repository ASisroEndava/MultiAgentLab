using MultiAgentLab.Api.Application.Supervisor;
using MultiAgentLab.Api.Domain;

namespace MultiAgentLab.Tests;

public class SupervisorTests
{
    private readonly AgentSelectionRules _rules = new();
    private readonly ConflictResolver _conflictResolver = new();

    [Fact]
    public void SimpleTextChange_ShouldInvokeClarityAndUx()
    {
        var request = new ReviewRequest
        {
            StoryId = "test-001",
            Title = "Cambiar texto del boton",
            StoryText = "Como usuario, quiero que el boton \"Guardar\" diga \"Confirmar\".",
            Provider = new ProviderSelection { Type = "ollama", Model = "llama3.1" }
        };

        var selection = _rules.Select(request);

        Assert.Contains("clarity", selection.Invoked);
        Assert.Contains("ux", selection.Invoked);
        Assert.DoesNotContain("technical", selection.Invoked);
        Assert.DoesNotContain("compliance", selection.Invoked);
    }

    [Fact]
    public void BackendStory_ShouldInvokeTechnical()
    {
        var request = new ReviewRequest
        {
            StoryId = "test-003",
            Title = "Reintentos automaticos",
            StoryText = "Como sistema, necesito reintentar automaticamente el envio de notificaciones fallidas hasta 3 veces.",
            Provider = new ProviderSelection { Type = "ollama", Model = "llama3.1" }
        };

        var selection = _rules.Select(request);

        Assert.Contains("clarity", selection.Invoked);
        Assert.Contains("technical", selection.Invoked);
        Assert.DoesNotContain("ux", selection.Invoked);
    }

    [Fact]
    public void PersonalDataStory_ShouldInvokeCompliance()
    {
        var request = new ReviewRequest
        {
            StoryId = "test-004",
            Title = "Descargar reporte personal",
            StoryText = "Como cliente, quiero descargar un reporte con mis datos personales y transacciones del ultimo ano.",
            Provider = new ProviderSelection { Type = "ollama", Model = "llama3.1" }
        };

        var selection = _rules.Select(request);

        Assert.Contains("clarity", selection.Invoked);
        Assert.Contains("compliance", selection.Invoked);
    }

    [Fact]
    public void UiStory_ShouldInvokeUxAndQa()
    {
        var request = new ReviewRequest
        {
            StoryId = "test-002",
            Title = "Resetear contrasena",
            StoryText = "Como usuario, quiero poder resetear mi contrasena desde la pantalla de login para recuperar acceso.",
            Provider = new ProviderSelection { Type = "ollama", Model = "llama3.1" }
        };

        var selection = _rules.Select(request);

        Assert.Contains("clarity", selection.Invoked);
        Assert.Contains("ux", selection.Invoked);
        Assert.Contains("qa", selection.Invoked);
    }

    [Fact]
    public void ConflictResolver_UxVsTechnical_DetectsConflict()
    {
        var results = new List<AgentResult>
        {
            new AgentResult
            {
                Agent = "ux",
                Status = "ok",
                Issues = new List<string> { "El feedback es lento" },
                Recommendations = new List<string> { "Permitir edicion inline inmediata y simple" }
            },
            new AgentResult
            {
                Agent = "technical",
                Status = "ok",
                Issues = new List<string> { "Cambiar direccion puede tener impacto en pedidos por estado del proceso" },
                Recommendations = new List<string> { "Validar estado del pedido" }
            }
        };

        var (conflicts, resolutions) = _conflictResolver.Detect(results);

        Assert.NotEmpty(conflicts);
        Assert.NotEmpty(resolutions);
    }

    [Fact]
    public void ConflictResolver_NoConflict_ReturnsEmpty()
    {
        var results = new List<AgentResult>
        {
            new AgentResult
            {
                Agent = "clarity",
                Status = "ok",
                Issues = new List<string> { "Falta definicion de caso de error" }
            }
        };

        var (conflicts, resolutions) = _conflictResolver.Detect(results);

        Assert.Empty(conflicts);
        Assert.Empty(resolutions);
    }

    [Fact]
    public void SkippedAgents_HaveReasons()
    {
        var request = new ReviewRequest
        {
            StoryId = "test-001",
            Title = "Cambiar texto del boton",
            StoryText = "Como usuario, quiero que el boton \"Guardar\" diga \"Confirmar\".",
            Provider = new ProviderSelection { Type = "ollama", Model = "llama3.1" }
        };

        var selection = _rules.Select(request);

        foreach (var skipped in selection.Skipped)
        {
            Assert.False(string.IsNullOrWhiteSpace(skipped.Reason),
                $"Skipped agent '{skipped.Agent}' should have a reason");
        }
    }
}
