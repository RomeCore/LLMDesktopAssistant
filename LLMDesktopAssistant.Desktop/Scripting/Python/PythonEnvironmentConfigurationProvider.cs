using System.ComponentModel;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;

namespace LLMDesktopAssistant.Desktop.Scripting.Python
{
	[Service(typeof(IScriptEngineEnvConfigurationProvider))]
	public class PythonEnvironmentConfigurationProvider(IExplorerOpener explorerOpener) : IScriptEngineEnvConfigurationProvider
	{
		public ScriptLanguageType Language => ScriptLanguageType.Python;

		public AdditionalEnvironmentSetting CreateConfiguration()
		{
			return new PythonEnvironmentConfiguration();
		}

		public INotifyPropertyChanged CreateViewModel(AdditionalEnvironmentSetting configuration)
		{
			return new PythonEnvironmentConfigurationViewModel((PythonEnvironmentConfiguration)configuration, explorerOpener);
		}

		public AdditionalEnvironmentSetting? FindConfiguration(IEnumerable<AdditionalEnvironmentSetting> existingSettings)
		{
			return existingSettings.OfType<PythonEnvironmentConfiguration>().FirstOrDefault();
		}
	}
}
