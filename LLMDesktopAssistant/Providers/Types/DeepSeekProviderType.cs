using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients.Deepseek;
using RCLargeLanguageModels.Security;

namespace LLMDesktopAssistant.Providers.Types
{
	[ModelProviderType]
	public class DeepSeekProviderType : ModelProviderType
	{
		public override string Id => "deepseek";

		public override string? DefaultEndpoint => DeepSeekClient.BaseUri;

		public override bool? IsApiKeyRequired(ModelProviderConfiguration providerConfig)
		{
			return true;
		}

		public override LLMClient CreateClient(ModelProviderConfiguration providerConfig, ITokenAccessor? tokenAccessor)
		{
			var endpoint = providerConfig.EndpointUri ?? DeepSeekClient.BaseUri;
			return new DeepSeekClient(endpoint, tokenAccessor!);
		}
	}
}
