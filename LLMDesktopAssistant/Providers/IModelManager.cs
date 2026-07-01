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
		/// <param name="fullName">The full name of the model in format {ClientName}${ModelName}, openai$gpt-3.5-turbo for example.</param>
		/// <returns>True if the model is available, otherwise false.</returns>
		bool IsModelAvaliable(string fullName);

		/// <summary>
		/// Gets a model instance by its full name. The model is ready for running.
		/// </summary>
		/// <param name="fullName">The full name of the model in format {ClientName}${ModelName}, openai$gpt-3.5-turbo for example.</param>
		/// <returns>A model instance if available, otherwise null.</returns>
		LLModel GetModel(string fullName);

		/// <summary>
		/// Lists all models that are currently available.
		/// </summary>
		/// <returns>A collection of model items. Each item represents a model with its full name and other details.</returns>
		IEnumerable<ModelItem> ListModels();

		/// <summary>
		/// Refreshes the list of available models for a specific model provider.
		/// Overrides the models listed in <see cref="ModelProviderConfiguration.Models"/>.
		/// </summary>
		/// <param name="provider">The model provider configuration to refresh.</param>
		void RefreshModels(ModelProviderConfiguration provider);
	}

	public class ModelManager : IModelManager
	{
		public bool IsModelAvaliable(string fullName)
		{
			throw new NotImplementedException();
		}

		public LLModel GetModel(string fullName)
		{
			throw new NotImplementedException();
		}

		public IEnumerable<ModelItem> ListModels()
		{
			throw new NotImplementedException();
		}

		public void RefreshModels(ModelProviderConfiguration provider)
		{
			throw new NotImplementedException();
		}

		private static (string ClientName, string ModelName) ParseFullName(string fullName)
		{
			var split = fullName.Split('$', count: 2);
			if (split.Length < 2)
				throw new ArgumentException("Invalid model full name format.", nameof(fullName));
			return (ClientName: split[0], ModelName: split[1]);
		}
	}
}
