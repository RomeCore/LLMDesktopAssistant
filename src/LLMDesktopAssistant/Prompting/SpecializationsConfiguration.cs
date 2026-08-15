using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Prompting
{
	public class SpecializationsConfiguration : SettingsObject
	{
		private readonly RangeObservableCollection<Specialization> _specializations = [];
		/// <summary>
		/// Gets or sets the list of specializations.
		/// </summary>
		public ICollection<Specialization> Specializations
		{
			get => _specializations;
			set => _specializations.Reset(value);
		}
	}
}
