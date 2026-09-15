using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Addons
{
	public class AddonSetConfigurationBase<TChange> : SettingsObject
		where TChange : AddonChangeBase
	{
		public bool EnabledByDefault
		{
			get;
			set => SetProperty(ref field, value);
		} = true;

		public bool HiddenByDefault
		{
			get;
			set => SetProperty(ref field, value);
		} = false;

		public ObservableDictionary<string, TChange> Changes
		{
			get => field ??= [];
			set => (field ??= []).Reset(value);
		}
	}
}
