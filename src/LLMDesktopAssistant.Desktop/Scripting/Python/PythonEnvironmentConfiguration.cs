using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Utils.Json;

namespace LLMDesktopAssistant.Desktop.Scripting.Python
{
	[JsonDerived(typeof(AdditionalEnvironmentSetting), "python")]
	public class PythonEnvironmentConfiguration : AdditionalEnvironmentSetting
	{
		private string? _pythonVenvActivateScriptPath;
		/// <summary>
		/// The path to the script that activates a python virtual environment.
		/// </summary>
		public string? PythonVenvActivateScriptPath
		{
			get => _pythonVenvActivateScriptPath;
			set => SetProperty(ref _pythonVenvActivateScriptPath, value);
		}

		private string? _pythonMetaVenvActivateScriptPath;
		/// <summary>
		/// The path to the script that activates a python virtual environment.
		/// Used for meta-tools. If null, the <see cref="PythonVenvActivateScriptPath"/> will be used.
		/// </summary>
		public string? PythonMetaVenvActivateScriptPath
		{
			get => _pythonMetaVenvActivateScriptPath;
			set => SetProperty(ref _pythonMetaVenvActivateScriptPath, value);
		}
	}
}
