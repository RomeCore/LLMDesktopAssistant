using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients.Novita;
using RCLargeLanguageModels.Security;

namespace LLMDesktopAssistant.Providers.Types
{
	[ModelProviderType]
	public class NovitaProviderType : ModelProviderType
	{
		public override string Id => "novita";

		public override ModelProviderConfiguration CreateDefaultConfiguration()
		{
			return new ModelProviderConfiguration
			{
				EndpointUri = NovitaClient.BaseUri
			};
		}

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
