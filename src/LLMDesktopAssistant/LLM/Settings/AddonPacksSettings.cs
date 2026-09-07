using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	public class AddonPacksSettings : NotifyPropertyChanged
	{
		/// <summary>
		/// A boolean indicating whether to enable packs by default.
		/// For example, if this value is false, all UNCHANGED packs will be disabled by default (see <see cref="EnabledPacks"/>).
		/// </summary>
		public bool EnablePacksByDefault
		{
			get;
			set => SetProperty(ref field, value);
		}

		/// <summary>
		/// A dictionary of enabled packs, where the key is the pack path and the value is a boolean indicating whether the pack is enabled.
		/// Not existing keys means to use <see cref="EnablePacksByDefault"/>.
		/// </summary>
		public ObservableDictionary<string, bool> EnabledPacks
		{
			get => field ??= [];
			set => (field ??= []).Reset(value);
		}
	}
}
