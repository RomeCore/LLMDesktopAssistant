using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Prompting
{
	public class PromptComponentsConfiguration : SettingsObject
	{
		private readonly RangeObservableCollection<PromptComponent> _components = [];
		/// <summary>
		/// Gest or sets the list of prompt components.
		/// </summary>
		public ICollection<PromptComponent> Components
		{
			get => _components;
			set => _components.Reset(value);
		}
	}
}
