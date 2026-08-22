using RCLargeLanguageModels;

namespace LLMDesktopAssistant.Providers
{
	public interface IModelManager
	{
		/// <summary>
		/// Checks if a model is available.
		/// </summary>
		/// <param name="fullName">The full name of the model in format {Provider}${Model} or {Provider}${Model}${Modifier},
		/// OpenAI$gpt-3.5-turbo or DeepSeek$deepseek-v4-flash$Max for example.</param>
		/// <returns>True if the model is available, otherwise false.</returns>
		bool IsModelAvaliable(string fullName);

		/// <summary>
		/// Gets a model instance by its full name. The model is ready for running.
		/// If the full name contains a modifier, its generation parameters are applied to the model.
		/// </summary>
		/// <param name="fullName">The full name of the model in format {Provider}${Model} or {Provider}${Model}${Modifier},
		/// OpenAI$gpt-3.5-turbo or DeepSeek$deepseek-v4-flash$Max for example.</param>
		/// <returns>A model instance if available, otherwise throws an exception.</returns>
		LLModel GetModel(string fullName);

		/// <summary>
		/// Tries to get a model instance by its full name. The model is ready for running.
		/// If the full name contains a modifier, its generation parameters are applied to the model.
		/// </summary>
		/// <param name="fullName">The full name of the model in format {Provider}${Model} or {Provider}${Model}${Modifier},
		/// OpenAI$gpt-3.5-turbo or DeepSeek$deepseek-v4-flash$Max for example.</param>
		/// <returns>A model instance if available, otherwise null.</returns>
		LLModel? TryGetModel(string fullName);

		/// <summary>
		/// Lists all models that are currently available.
		/// </summary>
		/// <returns>A collection of model items. Each item represents a model with its full name and other details.</returns>
		IEnumerable<ModelItem> ListModels();

		/// <summary>
		/// Lists all selected models (from <see cref="ModelProviderConfiguration.SelectedModelNames"/> and <see cref="ModelProviderConfiguration.CustomModels"/>).
		/// </summary>
		/// <returns>A collection of model items filtered by selection.</returns>
		IEnumerable<ModelItem> ListSelectedModels();

		/// <summary>
		/// Lists all available model modifiers.
		/// </summary>
		/// <returns>A collection of model modifiers.</returns>
		IReadOnlyList<ModelModifier> ListModifiers();

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
