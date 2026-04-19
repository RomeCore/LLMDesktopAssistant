using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.ToolModules;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
	public class MetaToolConfiguration : SettingsObject
	{
		private readonly RangeObservableCollection<MetaTool> _tools = [];
		/// <summary>
		/// Collection of tools that can be tweaked and called by LLM.
		/// </summary>
		public ICollection<MetaTool> Tools
		{
			get => _tools;
			set => _tools.Reset(value);
		}
	}
}