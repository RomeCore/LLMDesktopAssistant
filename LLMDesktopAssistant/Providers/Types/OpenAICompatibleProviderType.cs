using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients.Ollama;
using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Security;

namespace LLMDesktopAssistant.Providers.Types
{
	[ModelProviderType]
	public class OpenAICompatibleProviderType : ModelProviderType
	{
		public override string Id => "openai-compat";

		public override string? DefaultEndpoint => null;

		public override bool? IsApiKeyRequired(ModelProviderConfiguration providerConfig)
		{
			return null; // Api key may not be required (depends on specific provider)
		}

		public override LLMClient CreateClient(ModelProviderConfiguration providerConfig, ITokenAccessor? tokenAccessor)
		{
			if (providerConfig.EndpointUri == null)
				throw new ArgumentNullException(nameof(providerConfig.EndpointUri), "Endpoint URI is required for OpenAI-compatible provider.");
			return new OpenAICompatibleClient(providerConfig.EndpointUri!, tokenAccessor!);
		}
	}
}
