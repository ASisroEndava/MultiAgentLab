using MultiAgentLab.Api.Domain;

namespace MultiAgentLab.Api.Infrastructure.LLM;

public sealed class ModelRouter : IModelRouter
{
    private readonly IHttpClientFactory _httpClientFactory;

    public ModelRouter(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public IModelClient Resolve(ProviderSelection providerSelection)
    {
        return providerSelection.Type.ToLowerInvariant() switch
        {
            "bedrock" => new BedrockClient(),
            "ollama"  => new OllamaClient(_httpClientFactory),
            _ => throw new ArgumentException($"Unknown provider type: {providerSelection.Type}")
        };
    }
}
