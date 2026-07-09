using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients.Deepseek;
using RCLargeLanguageModels.Clients.Novita;
using RCLargeLanguageModels.Security;

namespace LLMDesktopAssistant.Providers.Types
{
	[ModelProviderType]
	public class NovitaProviderType : ModelProviderType
	{
		public override string Id => "novita";

		public override string? DefaultEndpoint => NovitaClient.BaseUri;

		public override bool? IsApiKeyRequired(ModelProviderConfiguration providerConfig)
		{
			return true;
		}

		public override LLMClient CreateClient(ModelProviderConfiguration providerConfig, ITokenAccessor? tokenAccessor)
		{
			var endpoint = providerConfig.EndpointUri ?? NovitaClient.BaseUri;
			return new NovitaClient(endpoint, tokenAccessor!);
		}
	}
}
