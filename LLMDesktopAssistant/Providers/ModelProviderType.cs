using System;
using System.Collections.Generic;
using System.Text;
using LLMDesktopAssistant.Localization;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Security;

namespace LLMDesktopAssistant.Providers
{
	public abstract class ModelProviderType
	{
		/// <summary>
		/// Gets the unique identifier for this provider type.
		/// </summary>
		public abstract string Id { get; }

		/// <summary>
		/// Gets the display name of this provider type, localized via key "model_provider_{Id}".
		/// Falls back to <see cref="Id"/> if localization is not found.
		/// </summary>
		public string DisplayName => LocalizationManager.LocalizeStatic("model_provider_" + Id);

		/// <summary>
		/// Gets the default endpoint URL for this model provider type.
		/// </summary>
		public abstract string? DefaultEndpoint { get; }

		/// <summary>
		/// Determines whether an API key is required for the given provider configuration.
		/// Returns <see langword="true"/> if required, <see langword="false"/> if not required,
		/// <see langword="null"/> if unknown (optional).
		/// </summary>
		public abstract bool? IsApiKeyRequired(ModelProviderConfiguration providerConfig);

		/// <summary>
		/// Creates an LLM client for this provider type.
		/// </summary>
		public abstract LLMClient CreateClient(ModelProviderConfiguration providerConfig, ITokenAccessor? tokenAccessor);
	}
}
