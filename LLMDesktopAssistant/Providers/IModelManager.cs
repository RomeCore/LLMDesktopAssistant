using System;
using System.Collections.Generic;
using System.Text;
using RCLargeLanguageModels;

namespace LLMDesktopAssistant.Providers
{
	public interface IModelManager
	{
		/// <summary>
		/// Checks if a model is available.
		/// </summary>
		/// <param name="fullName">The full name of the model in format {ClientName}${ModelName}, OpenAI$gpt-3.5-turbo for example.</param>
		/// <returns>True if the model is available, otherwise false.</returns>
		bool IsModelAvaliable(string fullName);

		/// <summary>
		/// Gets a model instance by its full name. The model is ready for running.
		/// </summary>
		/// <param name="fullName">The full name of the model in format {ClientName}${ModelName}, OpenAI$gpt-3.5-turbo for example.</param>
		/// <returns>A model instance if available, otherwise throws an exception.</returns>
		LLModel GetModel(string fullName);

		/// <summary>
		/// Lists all models that are currently available.
		/// </summary>
		/// <returns>A collection of model items. Each item represents a model with its full name and other details.</returns>
		IEnumerable<ModelItem> ListModels();

		/// <summary>
		/// Checks if a connection to a model provider is available and configuration is valid.
		/// </summary>
		/// <param name="provider">The model provider configuration to check.</param>
		Task<bool> CheckConnectionAsync(ModelProviderConfiguration provider, CancellationToken cancellationToken = default);

		/// <summary>
		/// Refreshes the list of available models for a specific model provider.
		/// Overrides the models listed in <see cref="ModelProviderConfiguration.Models"/>.
		/// </summary>
		/// <param name="provider">The model provider configuration to refresh.</param>
		Task RefreshModelsAsync(ModelProviderConfiguration provider, CancellationToken cancellationToken = default);
	}
}
