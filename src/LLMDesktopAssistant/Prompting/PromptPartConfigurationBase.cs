using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Prompting
{
	public abstract class PromptPartConfigurationBase<T> : SettingsObject
		where T : PromptPartBase
	{
		private readonly RangeObservableCollection<T> _parts = [];
		/// <summary>
		/// Gets or sets the list of prompt parts.
		/// </summary>
		public RangeObservableCollection<T> PromptParts
		{
			get => _parts;
			set => _parts.Reset(value);
		}
	}
}
