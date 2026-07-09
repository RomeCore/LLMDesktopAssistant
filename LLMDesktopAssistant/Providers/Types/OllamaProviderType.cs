using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients.Deepseek;
using RCLargeLanguageModels.Clients.Ollama;
using RCLargeLanguageModels.Security;

namespace LLMDesktopAssistant.Providers.Types
{
	[ModelProviderType]
	public class OllamaProviderType : ModelProviderType
	{
		public override string Id => "ollama";

		public override ModelProviderConfiguration CreateDefaultConfiguration()
		{
			return new ModelProviderConfiguration
			{
				EndpointUri = OllamaClient.DefaultBaseUri
			};
		}

		public override bool? IsApiKeyRequired(ModelProviderConfiguration providerConfig)
		{
			// Check if the endpoint is a loopback address (locally hosted), so the API key is not required.
			if (providerConfig.EndpointUri != null && new Uri(providerConfig.EndpointUri).IsLoopback)
				return false;
			return null;
		}

		public override LLMClient CreateClient(ModelProviderConfiguration providerConfig, ITokenAccessor? tokenAccessor)
		{
			var endpoint = providerConfig.EndpointUri ?? OllamaClient.DefaultBaseUri;
			if (tokenAccessor != null)
				return new OllamaClient(endpoint, tokenAccessor);
			return new OllamaClient(endpoint);
		}
	}
}
