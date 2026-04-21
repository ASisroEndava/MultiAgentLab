using System.Collections.Concurrent;
using System.Text.Json;
using MultiAgentLab.Api.Domain;

namespace MultiAgentLab.Api.Infrastructure.Logging;

public sealed class JsonlExecutionLogger : IExecutionLogger
{
    private readonly string _logDirectory;
    private readonly ConcurrentDictionary<string, List<ExecutionLogEvent>> _inMemoryLogs = new();
    private static readonly SemaphoreSlim _fileLock = new(1, 1);

    public JsonlExecutionLogger(string? logDirectory = null)
    {
        _logDirectory = logDirectory ?? Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(_logDirectory);
    }

    public async Task LogAsync(ExecutionLogEvent logEvent, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeToJsonElement(logEvent);

        _inMemoryLogs.AddOrUpdate(
            normalized.ExecutionId,
            _ => new List<ExecutionLogEvent> { normalized },
            (_, list) => { list.Add(normalized); return list; });

        var filePath = Path.Combine(_logDirectory, $"{normalized.ExecutionId}.jsonl");
        var json = JsonSerializer.Serialize(normalized, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        await _fileLock.WaitAsync(cancellationToken);
        try
        {
            await File.AppendAllTextAsync(filePath, json + Environment.NewLine, cancellationToken);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public Task<List<ExecutionLogEvent>> GetLogsAsync(string executionId, CancellationToken cancellationToken = default)
    {
        if (_inMemoryLogs.TryGetValue(executionId, out var logs))
        {
            return Task.FromResult(logs.ToList());
        }

        var filePath = Path.Combine(_logDirectory, $"{executionId}.jsonl");
        if (!File.Exists(filePath))
        {
            return Task.FromResult(new List<ExecutionLogEvent>());
        }

        var lines = File.ReadAllLines(filePath);
        var events = lines
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => JsonSerializer.Deserialize<ExecutionLogEvent>(l)!)
            .ToList();

        return Task.FromResult(events);
    }

    private static ExecutionLogEvent NormalizeToJsonElement(ExecutionLogEvent logEvent)
    {
        if (logEvent.Data is JsonElement)
            return logEvent;

        var dataJson = JsonSerializer.Serialize(logEvent.Data);
        var dataElement = JsonSerializer.Deserialize<JsonElement>(dataJson);

        return new ExecutionLogEvent
        {
            ExecutionId = logEvent.ExecutionId,
            Timestamp = logEvent.Timestamp,
            EventType = logEvent.EventType,
            Data = dataElement
        };
    }

    public Task<List<string>> GetAllExecutionIdsAsync(CancellationToken cancellationToken = default)
    {
        var ids = new HashSet<string>(_inMemoryLogs.Keys);

        if (Directory.Exists(_logDirectory))
        {
            foreach (var file in Directory.GetFiles(_logDirectory, "*.jsonl"))
            {
                var name = Path.GetFileNameWithoutExtension(file);
                ids.Add(name);
            }
        }

        var sorted = ids.OrderByDescending(id => id).ToList();
        return Task.FromResult(sorted);
    }
}
