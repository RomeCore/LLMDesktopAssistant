using LLMDesktopAssistant.Core.Settings;
using LLMDesktopAssistant.Core.ToolModules;
using LLMDesktopAssistant.Core.Utils;

namespace LLMDesktopAssistant.Core.LLM.Services.Tools
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