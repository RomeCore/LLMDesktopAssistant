using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients.OpenRouter;
using RCLargeLanguageModels.Security;

namespace LLMDesktopAssistant.Providers.Types
{
	[ModelProviderType]
	public class OpenRouterProviderType : ModelProviderType
	{
		public override string Id => "openrouter";

		public override ModelProviderConfiguration CreateDefaultConfiguration()
		{
			return new ModelProviderConfiguration
			{
				EndpointUri = OpenRouterClient.BaseUri
			};
		}

		public override bool? IsApiKeyRequired(ModelProviderConfiguration providerConfig)
		{
			return true;
		}

		public override LLMClient CreateClient(ModelProviderConfiguration providerConfig, ITokenAccessor? tokenAccessor)
		{
			var endpoint = providerConfig.EndpointUri ?? OpenRouterClient.BaseUri;
			return new OpenRouterClient(endpoint, tokenAccessor!);
		}
	}
}
