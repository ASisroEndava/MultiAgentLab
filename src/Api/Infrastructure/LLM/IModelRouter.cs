using MultiAgentLab.Api.Domain;

namespace MultiAgentLab.Api.Infrastructure.LLM;

public interface IModelRouter
{
    IModelClient Resolve(ProviderSelection providerSelection);
}
