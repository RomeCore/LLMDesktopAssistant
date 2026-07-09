using System;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Security;

namespace LLMDesktopAssistant.Providers
{
	public abstract class ModelProviderType
	{
		public abstract string Id { get; }

		public abstract ModelProviderConfiguration CreateDefaultConfiguration();

		public abstract bool? IsApiKeyRequired(ModelProviderConfiguration providerConfig);

		public abstract LLMClient CreateClient(ModelProviderConfiguration providerConfig, ITokenAccessor? tokenAccessor);
	}
}
