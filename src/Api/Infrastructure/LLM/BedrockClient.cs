using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Amazon;
using Amazon.BedrockRuntime;
using Amazon.BedrockRuntime.Model;
using Amazon.Runtime.CredentialManagement;
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
        var profileName = Environment.GetEnvironmentVariable("AWS_PROFILE") ?? "419466290453_AdministratorAccess";
        var chain = new CredentialProfileStoreChain();
        using var client = chain.TryGetAWSCredentials(profileName, out var credentials)
            ? new AmazonBedrockRuntimeClient(credentials, region)
            : new AmazonBedrockRuntimeClient(region);

        var modelId = request.Provider.Model;
        var payloadJson = BuildPayload(StripRegionPrefix(modelId), request);

        var invokeRequest = new InvokeModelRequest
        {
            ModelId = modelId,
            ContentType = "application/json",
            Accept = "application/json",
            Body = new MemoryStream(Encoding.UTF8.GetBytes(payloadJson))
        };

        var response = await client.InvokeModelAsync(invokeRequest, cancellationToken);

        using var reader = new StreamReader(response.Body);
        var responseJson = await reader.ReadToEndAsync(cancellationToken);

        sw.Stop();

        var (text, tokensUsed) = ExtractResponse(StripRegionPrefix(modelId), responseJson);

        return new ModelResponse
        {
            Text = text,
            TokensUsed = tokensUsed,
            LatencyMs = sw.Elapsed.TotalMilliseconds
        };
    }

    private static string StripRegionPrefix(string modelId) =>
        Regex.Replace(modelId, @"^(us|eu|ap)\.", string.Empty);

    private static string BuildPayload(string modelId, ModelRequest request) => modelId switch
    {
        var m when m.StartsWith("anthropic.") => JsonSerializer.Serialize(new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = request.Provider.MaxTokens ?? 8192,
            temperature = request.Provider.Temperature,
            messages = new[] { new { role = "user", content = request.Prompt } }
        }),
        var m when m.StartsWith("amazon.nova") => JsonSerializer.Serialize(new
        {
            messages = new[] { new { role = "user", content = new[] { new { text = request.Prompt } } } },
            inferenceConfig = new
            {
                maxTokens = request.Provider.MaxTokens ?? 8192,
                temperature = request.Provider.Temperature,
            }
        }),
        var m when m.StartsWith("amazon.titan-text") => JsonSerializer.Serialize(new
        {
            inputText = request.Prompt,
            textGenerationConfig = new
            {
                maxTokenCount = request.Provider.MaxTokens ?? 8192,
                temperature = request.Provider.Temperature,
                topP = 0.9
            }
        }),
        var m when m.StartsWith("meta.llama") => JsonSerializer.Serialize(new
        {
            prompt = request.Prompt,
            max_gen_len = request.Provider.MaxTokens ?? 8192,
            temperature = request.Provider.Temperature,
            top_p = 0.9
        }),
        var m when m.StartsWith("mistral.") => JsonSerializer.Serialize(new
        {
            prompt = $"<s>[INST] {request.Prompt} [/INST]",
            max_tokens = request.Provider.MaxTokens ?? 8192,
            temperature = request.Provider.Temperature,
            top_p = 0.9
        }),
        _ => JsonSerializer.Serialize(new
        {
            anthropic_version = "bedrock-2023-05-31",
            max_tokens = request.Provider.MaxTokens ?? 8192,
            temperature = request.Provider.Temperature,
            messages = new[] { new { role = "user", content = request.Prompt } }
        })
    };

    private static (string text, int tokens) ExtractResponse(string modelId, string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        if (modelId.StartsWith("anthropic."))
        {
            var text = root.TryGetProperty("content", out var content)
                ? content.EnumerateArray().FirstOrDefault().TryGetProperty("text", out var t) ? t.GetString() ?? "" : ""
                : "";
            var tokens = 0;
            if (root.TryGetProperty("usage", out var usage))
            {
                tokens += usage.TryGetProperty("input_tokens", out var i) ? i.GetInt32() : 0;
                tokens += usage.TryGetProperty("output_tokens", out var o) ? o.GetInt32() : 0;
            }
            return (text, tokens);
        }

        if (modelId.StartsWith("amazon.nova"))
        {
            var text = "";
            if (root.TryGetProperty("output", out var output)
                && output.TryGetProperty("message", out var message)
                && message.TryGetProperty("content", out var content))
            {
                text = content.EnumerateArray().FirstOrDefault()
                    .TryGetProperty("text", out var t) ? t.GetString() ?? "" : "";
            }
            var tokens = 0;
            if (root.TryGetProperty("usage", out var usage))
            {
                tokens += usage.TryGetProperty("inputTokens", out var i) ? i.GetInt32() : 0;
                tokens += usage.TryGetProperty("outputTokens", out var o) ? o.GetInt32() : 0;
            }
            return (text, tokens);
        }

        if (modelId.StartsWith("amazon.titan-text"))
        {
            var text = root.TryGetProperty("results", out var results)
                ? results.EnumerateArray().FirstOrDefault().TryGetProperty("outputText", out var t) ? t.GetString() ?? "" : ""
                : "";
            var inputTokens = root.TryGetProperty("inputTextTokenCount", out var it) ? it.GetInt32() : 0;
            var outputTokens = root.TryGetProperty("results", out var r2)
                ? r2.EnumerateArray().FirstOrDefault().TryGetProperty("tokenCount", out var tc) ? tc.GetInt32() : 0
                : 0;
            return (text, inputTokens + outputTokens);
        }

        if (modelId.StartsWith("meta.llama"))
        {
            var text = root.TryGetProperty("generation", out var gen) ? gen.GetString() ?? "" : "";
            var tokens = 0;
            tokens += root.TryGetProperty("prompt_token_count", out var pt) ? pt.GetInt32() : 0;
            tokens += root.TryGetProperty("generation_token_count", out var gt) ? gt.GetInt32() : 0;
            return (text, tokens);
        }

        if (modelId.StartsWith("mistral."))
        {
            var text = root.TryGetProperty("outputs", out var outputs)
                ? outputs.EnumerateArray().FirstOrDefault().TryGetProperty("text", out var t) ? t.GetString() ?? "" : ""
                : "";
            return (text, 0);
        }

        return ("", 0);
    }
}
