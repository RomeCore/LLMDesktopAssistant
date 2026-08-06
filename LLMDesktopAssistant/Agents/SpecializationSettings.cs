namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Represents the specialization group of an agent prompt: the selection between a
	/// registered specialization and a custom specialization text.
	/// </summary>
	public class SpecializationSettings : NotifyPropertyChanged
	{
		private bool _useCustomSpecialization = false;
		/// <summary>
		/// Whether to use a custom specialization. False for <see cref="SpecializationId"/>, true for <see cref="CustomSpecialization"/>.
		/// </summary>
		public bool UseCustomSpecialization
		{
			get => _useCustomSpecialization;
			set => SetProperty(ref _useCustomSpecialization, value);
		}

		private string? _customSpecialization;
		/// <summary>
		/// The custom specialization prompt to use, if not null or empty, this will be used instead of <see cref="SpecializationId"/>.
		/// </summary>
		public string? CustomSpecialization
		{
			get => _customSpecialization;
			set => SetProperty(ref _customSpecialization, value);
		}

		private Guid? _specializationId;
		/// <summary>
		/// The specialization ID for the agent. Defines the professional role/knowledge domain.
		/// The identifier leads to <see cref="Prompting.PromptRegistry.GetSpecialization(Guid)"/>
		/// </summary>
		public Guid? SpecializationId
		{
			get => _specializationId;
			set => SetProperty(ref _specializationId, value);
		}
	}
}
