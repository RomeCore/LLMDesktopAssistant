using System.ComponentModel;
using LLMDesktopAssistant.LLM.Settings;

namespace LLMDesktopAssistant.Scripting
{
	public interface IScriptEngineEnvConfigurationProvider
	{
		/// <summary>
		/// Gets the type of script language that this configuration provider is associated with.
		/// </summary>
		ScriptLanguageType Language { get; }

		/// <summary>
		/// Finds a configuration for additional environment settings based on the provided settings.
		/// </summary>
		/// <param name="existingSettings">The existing settings to search through.</param>
		/// <returns>The found or created additional environment setting, or null if no matching setting is found.</returns>
		AdditionalEnvironmentSetting? FindConfiguration(IEnumerable<AdditionalEnvironmentSetting> existingSettings);

		/// <summary>
		/// Creates a new instance of the environment settings configuration for the script engine.
		/// </summary>
		/// <returns>A new instance of the environment settings configuration.</returns>
		AdditionalEnvironmentSetting CreateConfiguration();

		/// <summary>
		/// Creates a view model for the given additional environment setting.
		/// </summary>
		/// <param name="configuration">The additional environment setting to create a view model for.</param>
		/// <returns>The view model for the given additional environment setting.</returns>
		INotifyPropertyChanged CreateViewModel(AdditionalEnvironmentSetting configuration);

		/// <summary>
		/// Checks the given additional environment setting for any issues or missing dependencies.
		/// </summary>
		/// <param name="settings">The chat environment settings to use for checking.</param>
		/// <param name="configuration">The additional environment setting to check.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
		/// <returns>The check result, indicating whether the configuration is valid or not.</returns>
		Task<ScriptEnvironmentCheckResult> CheckConfigurationAsync(ChatEnvironmentSettings settings,
			AdditionalEnvironmentSetting configuration, CancellationToken cancellationToken = default);
	}
}
