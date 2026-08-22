using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Providers
{
	/// <summary>
	/// The configuration of all available model modifiers.
	/// </summary>
	[SettingsObject("model_modifiers")]
	public class ModelModifiersConfiguration : SettingsObject
	{
		private RangeObservableCollection<ModelModifier> _modifiers = [];
		/// <summary>
		/// Gets or sets the list of all available model modifiers.
		/// </summary>
		public RangeObservableCollection<ModelModifier> Modifiers
		{
			get => _modifiers;
			set => _modifiers.Reset(value);
		}
	}
}
