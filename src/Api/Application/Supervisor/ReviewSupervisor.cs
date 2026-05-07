using System.Diagnostics;
using MultiAgentLab.Api.Application.Agents;
using MultiAgentLab.Api.Domain;
using MultiAgentLab.Api.Infrastructure.Logging;

namespace MultiAgentLab.Api.Application.Supervisor;

public sealed class ReviewSupervisor
{
    private readonly Dictionary<string, IReviewAgent> _agents;
    private readonly AgentSelectionRules _selectionRules;
    private readonly ConflictResolver _conflictResolver;
    private readonly IExecutionLogger _logger;

    public ReviewSupervisor(
        IEnumerable<IReviewAgent> agents,
        AgentSelectionRules selectionRules,
        ConflictResolver conflictResolver,
        IExecutionLogger logger)
    {
        _agents = agents.ToDictionary(a => a.Name, a => a);
        _selectionRules = selectionRules;
        _conflictResolver = conflictResolver;
        _logger = logger;
    }

    public async Task<ReviewResult> ReviewAsync(
        ReviewRequest request,
        CancellationToken cancellationToken = default,
        string? preGeneratedExecutionId = null)
    {
        var sw = Stopwatch.StartNew();
        var executionId = preGeneratedExecutionId ?? $"exec-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid().ToString()[..8]}";

        await _logger.LogAsync(LogEvents.RequestReceived(executionId, request), cancellationToken);
        await _logger.LogAsync(LogEvents.SupervisorStarted(executionId), cancellationToken);

        var selection = _selectionRules.Select(request);
        await _logger.LogAsync(LogEvents.SelectedAgents(executionId, selection), cancellationToken);

        var context = new AgentContext
        {
            ExecutionId = executionId,
            StoryId = request.StoryId,
            Title = request.Title,
            StoryText = request.StoryText,
            Provider = request.Provider,
            Logging = request.Logging
        };

        var agentTasks = selection.Invoked
            .Where(name => _agents.ContainsKey(name))
            .Select(async name =>
            {
                var agent = _agents[name];
                try
                {
                    await _logger.LogAsync(LogEvents.AgentStarted(executionId, agent.Name), cancellationToken);
                    var result = await agent.ExecuteAsync(context, cancellationToken);
                    await _logger.LogAsync(LogEvents.AgentCompleted(executionId, result), cancellationToken);
                    return result;
                }
                catch (Exception ex)
                {
                    await _logger.LogAsync(LogEvents.AgentFailed(executionId, agent.Name, ex.Message), cancellationToken);
                    return new AgentResult
                    {
                        Agent = agent.Name,
                        Status = "error",
                        Issues = new List<string> { $"Error executing agent: {ex.Message}" },
                        RawSummary = ex.Message
                    };
                }
            })
            .ToList();

        var results = (await Task.WhenAll(agentTasks)).ToList();

        var (conflicts, resolutions) = _conflictResolver.Detect(results);

        if (conflicts.Count > 0)
        {
            await _logger.LogAsync(LogEvents.ConflictsDetected(executionId, conflicts), cancellationToken);
            await _logger.LogAsync(LogEvents.SupervisorResolution(executionId, resolutions), cancellationToken);
        }

        var allIssues = results.SelectMany(r => r.Issues).Distinct().ToList();
        var allRecommendations = results.SelectMany(r => r.Recommendations).Distinct().ToList();
        var status = DetermineStatus(results, conflicts);
        var summary = BuildSummary(results, conflicts);

        var reviewResult = new ReviewResult
        {
            ExecutionId = executionId,
            Status = status,
            Summary = summary,
            Provider = request.Provider.Type,
            Model = request.Provider.Model,
            InvokedAgents = selection.Invoked,
            SkippedAgents = selection.Skipped,
            Issues = allIssues,
            Recommendations = allRecommendations,
            Conflicts = conflicts,
            Resolution = resolutions,
            AgentResults = results
        };

        await _logger.LogAsync(LogEvents.FinalResultGenerated(executionId, reviewResult), cancellationToken);

        sw.Stop();
        await _logger.LogAsync(LogEvents.RequestCompleted(executionId, sw.Elapsed.TotalMilliseconds), cancellationToken);

        return reviewResult;
    }

    private static string DetermineStatus(List<AgentResult> results, List<string> conflicts)
    {
        var totalIssues = results.Sum(r => r.Issues.Count);
        var hasErrors = results.Any(r => r.Status == "error");
        var hasParseErrors = results.Any(r => r.Status == "parse_error");
        var avgScore = results.Where(r => r.Status == "ok").Select(r => r.Score).DefaultIfEmpty(0.5).Average();

        if (hasErrors || totalIssues > 6 || conflicts.Count >= 2)
            return "red";

        if (totalIssues > 2 || conflicts.Count >= 1 || avgScore < 0.6 || hasParseErrors)
            return "yellow";

        return "green";
    }

    private static string BuildSummary(List<AgentResult> results, List<string> conflicts)
    {
        var okCount = results.Count(r => r.Status == "ok");
        var totalIssues = results.Sum(r => r.Issues.Count);

        var parts = new List<string>();

        if (totalIssues == 0)
            parts.Add("The story appears complete and well-defined.");
        else if (totalIssues <= 2)
            parts.Add("The story is understandable but has minor observations.");
        else if (totalIssues <= 5)
            parts.Add("The story has missing definitions that should be completed.");
        else
            parts.Add("The story is incomplete and requires significant review.");

        if (conflicts.Count > 0)
            parts.Add($"{conflicts.Count} tension(s) between agents were detected and resolved by the supervisor.");

        parts.Add($"{okCount} of {results.Count} agents completed their review successfully.");

        return string.Join(" ", parts);
    }
}
