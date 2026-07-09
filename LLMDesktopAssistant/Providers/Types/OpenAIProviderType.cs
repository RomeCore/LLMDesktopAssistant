using RCLargeLanguageModels;
using RCLargeLanguageModels.Clients.OpenAI;
using RCLargeLanguageModels.Security;

namespace LLMDesktopAssistant.Providers.Types
{
	[ModelProviderType]
	public class OpenAIProviderType : ModelProviderType
	{
		public override string Id => "openai";

		public override ModelProviderConfiguration CreateDefaultConfiguration()
		{
			return new ModelProviderConfiguration
			{
				EndpointUri = OpenAIClient.BaseUri
			};
		}

		public override bool? IsApiKeyRequired(ModelProviderConfiguration providerConfig)
		{
			return true;
		}

		public override LLMClient CreateClient(ModelProviderConfiguration providerConfig, ITokenAccessor? tokenAccessor)
		{
			var endpoint = providerConfig.EndpointUri ?? OpenAIClient.BaseUri;
			return new OpenAIClient(endpoint, tokenAccessor!);
		}
	}
}
