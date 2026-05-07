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
            Title = "Change button text",
            StoryText = "As a user, I want the \"Save\" button to say \"Confirm\".",
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
            Title = "Automatic retries",
            StoryText = "As a system, I need to automatically retry sending failed notifications up to 3 times.",
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
            Title = "Download personal report",
            StoryText = "As a customer, I want to download a report with my personal data and transactions from the last year.",
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
            Title = "Reset password",
            StoryText = "As a user, I want to be able to reset my password from the login screen to recover access.",
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
                Issues = new List<string> { "The feedback is slow" },
                Recommendations = new List<string> { "Allow immediate and simple inline editing" }
            },
            new AgentResult
            {
                Agent = "technical",
                Status = "ok",
                Issues = new List<string> { "Changing the address may have an impact on orders due to process state" },
                Recommendations = new List<string> { "Validate order state" }
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
                Issues = new List<string> { "Missing error case definition" }
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
            Title = "Change button text",
            StoryText = "As a user, I want the \"Save\" button to say \"Confirm\".",
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
