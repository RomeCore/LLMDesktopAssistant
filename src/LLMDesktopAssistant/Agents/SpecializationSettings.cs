using LLMDesktopAssistant.Prompting;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Represents the specialization group of an agent prompt: the selection between a
	/// registered specialization and a custom specialization text.
	/// </summary>
	public class SpecializationSettings : PromptPartKeyedSelection<Guid>
	{
		private bool _useCustomSpecialization = false;
		/// <summary>
		/// Whether to use a custom specialization.
		/// </summary>
		public bool UseCustomSpecialization
		{
			get => _useCustomSpecialization;
			set => SetProperty(ref _useCustomSpecialization, value);
		}

		private string? _customSpecialization;
		/// <summary>
		/// The custom specialization prompt to use.
		/// </summary>
		public string? CustomSpecialization
		{
			get => _customSpecialization;
			set => SetProperty(ref _customSpecialization, value);
		}
	}
}
