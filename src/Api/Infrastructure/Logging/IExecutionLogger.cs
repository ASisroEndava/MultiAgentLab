using MultiAgentLab.Api.Domain;

namespace MultiAgentLab.Api.Infrastructure.Logging;

public interface IExecutionLogger
{
    Task LogAsync(ExecutionLogEvent logEvent, CancellationToken cancellationToken = default);
    Task<List<ExecutionLogEvent>> GetLogsAsync(string executionId, CancellationToken cancellationToken = default);
    Task<List<string>> GetAllExecutionIdsAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetExecutionIdsByStoryIdAsync(string storyId, CancellationToken cancellationToken = default);
    Task<ReviewResult?> GetFinalResultAsync(string executionId, CancellationToken cancellationToken = default);
}
