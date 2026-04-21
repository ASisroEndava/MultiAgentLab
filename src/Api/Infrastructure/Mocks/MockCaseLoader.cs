using System.Text.Json;
using MultiAgentLab.Api.Domain;

namespace MultiAgentLab.Api.Infrastructure.Mocks;

public sealed class MockCase
{
    public required string CaseId { get; init; }
    public required string Title { get; init; }
    public required string Description { get; init; }
    public required ReviewRequest Request { get; init; }
    public List<string> ExpectedAgents { get; init; } = new();
    public string ExpectedStatus { get; init; } = "amarillo";
}

public sealed class MockCaseLoader
{
    private readonly string _mockDirectory;
    private Dictionary<string, MockCase>? _cases;

    public MockCaseLoader(string? mockDirectory = null)
    {
        _mockDirectory = mockDirectory ?? Path.Combine(AppContext.BaseDirectory, "mock_inputs");
    }

    public async Task<List<MockCase>> ListCasesAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _cases!.Values.ToList();
    }

    public async Task<MockCase?> GetCaseAsync(string caseId, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _cases!.TryGetValue(caseId, out var c) ? c : null;
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_cases != null) return;

        _cases = new Dictionary<string, MockCase>();

        if (!Directory.Exists(_mockDirectory))
            return;

        var files = Directory.GetFiles(_mockDirectory, "*.json");
        foreach (var file in files.OrderBy(f => f))
        {
            try
            {
                var json = await File.ReadAllTextAsync(file, cancellationToken);
                var mockCase = JsonSerializer.Deserialize<MockCase>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (mockCase != null)
                {
                    _cases[mockCase.CaseId] = mockCase;
                }
            }
            catch
            {
                // Skip malformed files
            }
        }
    }
}
