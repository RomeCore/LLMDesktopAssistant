using LLMDesktopAssistant.Core.Settings;
using LLMDesktopAssistant.Core.Utils;

namespace LLMDesktopAssistant.Core.Prompting
{
	public class PersonasConfiguration : SettingsObject
	{
		private readonly RangeObservableCollection<Persona> _personas = [];
		/// <summary>
		/// Gest or sets the list of personas.
		/// </summary>
		public ICollection<Persona> Personas
		{
			get => _personas;
			set => _personas.Reset(value);
		}
	}
}
