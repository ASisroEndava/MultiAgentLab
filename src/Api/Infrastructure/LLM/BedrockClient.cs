using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using MultiAgentLab.Api.Domain;

namespace MultiAgentLab.Api.Infrastructure.LLM;

public sealed class BedrockClient : IModelClient
{
    public async Task<ModelResponse> GenerateAsync(
        ModelRequest request,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        var region = RegionEndpoint.GetBySystemName(request.Provider.Region ?? "us-east-1");
        using var client = new AmazonBedrockRuntimeClient(region);

        var payload = new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = request.Provider.MaxTokens ?? 2048,
            temperature = request.Provider.Temperature,
            messages = new[]
            {
                new { role = "user", content = request.Prompt }
            }
        };

        var payloadJson = JsonSerializer.Serialize(payload);

        var invokeRequest = new InvokeModelRequest
        {
            ModelId = request.Provider.Model,
            ContentType = "application/json",
            Accept = "application/json",
            Body = new MemoryStream(Encoding.UTF8.GetBytes(payloadJson))
        };

        var response = await client.InvokeModelAsync(invokeRequest, cancellationToken);

        using var reader = new StreamReader(response.Body);
        var responseJson = await reader.ReadToEndAsync(cancellationToken);
        var bedrockResponse = JsonSerializer.Deserialize<BedrockResponse>(responseJson);

        sw.Stop();

        var text = bedrockResponse?.Content?.FirstOrDefault()?.Text ?? string.Empty;
        var tokensUsed = (bedrockResponse?.Usage?.OutputTokens ?? 0) +
                         (bedrockResponse?.Usage?.InputTokens ?? 0);

        return new ModelResponse
        {
            Text = text,
            TokensUsed = tokensUsed,
            LatencyMs = sw.Elapsed.TotalMilliseconds
        };
    }

    private sealed class BedrockResponse
    {
        [JsonPropertyName("content")]
        public List<BedrockContent>? Content { get; set; }

        [JsonPropertyName("usage")]
        public BedrockUsage? Usage { get; set; }
    }

    private sealed class BedrockContent
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private sealed class BedrockUsage
    {
        [JsonPropertyName("input_tokens")]
        public int InputTokens { get; set; }

        [JsonPropertyName("output_tokens")]
        public int OutputTokens { get; set; }
    }
}
