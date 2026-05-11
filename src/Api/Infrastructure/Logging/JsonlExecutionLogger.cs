using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;
using MultiAgentLab.Api.Domain;

namespace MultiAgentLab.Api.Infrastructure.Logging;

public sealed class JsonlExecutionLogger : IExecutionLogger
{
    private readonly string _logDirectory;
    private readonly ConcurrentDictionary<string, List<ExecutionLogEvent>> _inMemoryLogs = new();
    private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _storyIndex = new();
    private static readonly SemaphoreSlim _fileLock = new(1, 1);
    private static readonly JsonSerializerOptions _readOptions = new() { PropertyNameCaseInsensitive = true };

    public JsonlExecutionLogger(string? logDirectory = null)
    {
        _logDirectory = logDirectory ?? Path.Combine(AppContext.BaseDirectory, "logs");
        Directory.CreateDirectory(_logDirectory);
        RebuildStoryIndex();
    }

    private void RebuildStoryIndex()
    {
        foreach (var filePath in Directory.GetFiles(_logDirectory, "*.jsonl"))
        {
            try
            {
                var firstLine = File.ReadLines(filePath).FirstOrDefault();
                if (firstLine is null) continue;
                var evt = JsonSerializer.Deserialize<ExecutionLogEvent>(firstLine, _readOptions);
                if (evt?.EventType != "request_received" || evt.Data is not JsonElement data) continue;
                if (!data.TryGetProperty("storyId", out var sid)) continue;
                var storyId = sid.GetString();
                if (string.IsNullOrWhiteSpace(storyId)) continue;
                var execId = Path.GetFileNameWithoutExtension(filePath);
                _storyIndex.GetOrAdd(storyId, _ => new ConcurrentBag<string>()).Add(execId);
            }
            catch { /* skip malformed files */ }
        }
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
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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

        if (normalized.EventType == "request_received" && normalized.Data is JsonElement reqData
            && reqData.TryGetProperty("storyId", out var storyIdProp))
        {
            var storyId = storyIdProp.GetString();
            if (!string.IsNullOrWhiteSpace(storyId))
                _storyIndex.GetOrAdd(storyId, _ => new ConcurrentBag<string>()).Add(normalized.ExecutionId);
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

        var dataJson = JsonSerializer.Serialize(logEvent.Data, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var dataElement = JsonSerializer.Deserialize<JsonElement>(dataJson);

        return new ExecutionLogEvent
        {
            ExecutionId = logEvent.ExecutionId,
            Timestamp = logEvent.Timestamp,
            EventType = logEvent.EventType,
            Data = dataElement
        };
    }

    public Task<List<string>> GetExecutionIdsByStoryIdAsync(string storyId, CancellationToken cancellationToken = default)
    {
        if (_storyIndex.TryGetValue(storyId, out var bag))
            return Task.FromResult(bag.Distinct().OrderByDescending(id => id).ToList());
        return Task.FromResult(new List<string>());
    }

    public async Task<ReviewResult?> GetFinalResultAsync(string executionId, CancellationToken cancellationToken = default)
    {
        var logs = await GetLogsAsync(executionId, cancellationToken);
        var finalEvent = logs.LastOrDefault(l => l.EventType == "final_result_generated");
        if (finalEvent?.Data is not JsonElement data) return null;
        return JsonSerializer.Deserialize<ReviewResult>(data.GetRawText(), _readOptions);
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
