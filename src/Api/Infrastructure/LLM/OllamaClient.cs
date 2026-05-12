using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MultiAgentLab.Api.Domain;

namespace MultiAgentLab.Api.Infrastructure.LLM;

public sealed class OllamaClient : IModelClient
{
    private readonly IHttpClientFactory _httpClientFactory;

    public OllamaClient(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<ModelResponse> GenerateAsync(
        ModelRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var client = _httpClientFactory.CreateClient("Ollama");
        client.Timeout = TimeSpan.FromMinutes(5);

        var endpoint = request.Provider.Endpoint?.TrimEnd('/') ?? "http://localhost:11434";
        var url = $"{endpoint}/api/generate";

        var body = new OllamaRequest
        {
            Model = request.Provider.Model,
            Prompt = request.Prompt,
            Stream = false,
            Options = new OllamaOptions
            {
                Temperature = request.Provider.Temperature,
                NumPredict = request.Provider.MaxTokens ?? 8192
            }
        };

        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var ollamaResponse = JsonSerializer.Deserialize<OllamaResponse>(responseJson);

        sw.Stop();

        return new ModelResponse
        {
            Text = ollamaResponse?.Response ?? string.Empty,
            TokensUsed = ollamaResponse?.EvalCount ?? 0,
            LatencyMs = sw.Elapsed.TotalMilliseconds
        };
    }

    private sealed class OllamaRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; set; }

        [JsonPropertyName("prompt")]
        public required string Prompt { get; set; }

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("options")]
        public OllamaOptions? Options { get; set; }
    }

    private sealed class OllamaOptions
    {
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; }

        [JsonPropertyName("num_predict")]
        public int NumPredict { get; set; }
    }

    private sealed class OllamaResponse
    {
        [JsonPropertyName("response")]
        public string? Response { get; set; }

        [JsonPropertyName("eval_count")]
        public int EvalCount { get; set; }
    }
}
