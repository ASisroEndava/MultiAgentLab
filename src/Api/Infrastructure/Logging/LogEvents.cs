using MultiAgentLab.Api.Domain;

namespace MultiAgentLab.Api.Infrastructure.Logging;

public static class LogEvents
{
    public static ExecutionLogEvent RequestReceived(string executionId, ReviewRequest request) => new()
    {
        ExecutionId = executionId,
        Timestamp = DateTimeOffset.UtcNow,
        EventType = "request_received",
        Data = new { storyId = request.StoryId, title = request.Title, storyText = request.StoryText, provider = request.Provider.Type, model = request.Provider.Model }
    };

    public static ExecutionLogEvent SupervisorStarted(string executionId) => new()
    {
        ExecutionId = executionId,
        Timestamp = DateTimeOffset.UtcNow,
        EventType = "supervisor_started",
        Data = new { }
    };

    public static ExecutionLogEvent SelectedAgents(string executionId, AgentSelection selection) => new()
    {
        ExecutionId = executionId,
        Timestamp = DateTimeOffset.UtcNow,
        EventType = "selected_agents",
        Data = new { invoked = selection.Invoked, skipped = selection.Skipped }
    };

    public static ExecutionLogEvent AgentStarted(string executionId, string agentName) => new()
    {
        ExecutionId = executionId,
        Timestamp = DateTimeOffset.UtcNow,
        EventType = "agent_started",
        Data = new { agent = agentName }
    };

    public static ExecutionLogEvent AgentPromptSent(string executionId, string agentName, string prompt) => new()
    {
        ExecutionId = executionId,
        Timestamp = DateTimeOffset.UtcNow,
        EventType = "agent_prompt_sent",
        Data = new { agent = agentName, prompt }
    };

    public static ExecutionLogEvent AgentResponseReceived(string executionId, string agentName, string response) => new()
    {
        ExecutionId = executionId,
        Timestamp = DateTimeOffset.UtcNow,
        EventType = "agent_response_received",
        Data = new { agent = agentName, response }
    };

    public static ExecutionLogEvent AgentCompleted(string executionId, AgentResult result) => new()
    {
        ExecutionId = executionId,
        Timestamp = DateTimeOffset.UtcNow,
        EventType = "agent_completed",
        Data = new { agent = result.Agent, status = result.Status, issues = result.Issues, score = result.Score }
    };

    public static ExecutionLogEvent AgentFailed(string executionId, string agentName, string error) => new()
    {
        ExecutionId = executionId,
        Timestamp = DateTimeOffset.UtcNow,
        EventType = "agent_failed",
        Data = new { agent = agentName, error }
    };

    public static ExecutionLogEvent ConflictsDetected(string executionId, List<string> conflicts) => new()
    {
        ExecutionId = executionId,
        Timestamp = DateTimeOffset.UtcNow,
        EventType = "conflict_detected",
        Data = new { conflicts }
    };

    public static ExecutionLogEvent SupervisorResolution(string executionId, List<string> resolution) => new()
    {
        ExecutionId = executionId,
        Timestamp = DateTimeOffset.UtcNow,
        EventType = "supervisor_resolution",
        Data = new { resolution }
    };

    public static ExecutionLogEvent FinalResultGenerated(string executionId, ReviewResult result) => new()
    {
        ExecutionId = executionId,
        Timestamp = DateTimeOffset.UtcNow,
        EventType = "final_result_generated",
        Data = new { status = result.Status, invokedAgents = result.InvokedAgents, issueCount = result.Issues.Count }
    };

    public static ExecutionLogEvent RequestCompleted(string executionId, double totalMs) => new()
    {
        ExecutionId = executionId,
        Timestamp = DateTimeOffset.UtcNow,
        EventType = "request_completed",
        Data = new { totalMs }
    };
}
