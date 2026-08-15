namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Represents the persona group of an agent prompt: the agent's nickname and the persona
	/// selection (a registered persona or a custom persona text).
	/// </summary>
	public class PersonaSettings : NotifyPropertyChanged
	{
		private string? _nickname;
		/// <summary>
		/// The agent's nickname to use in the chat.
		/// This affects how agent calls itself in the responses.
		/// </summary>
		public string? Nickname
		{
			get => _nickname;
			set => SetProperty(ref _nickname, value);
		}

		private bool _useCustomPersona = false;
		/// <summary>
		/// Whether to use a custom persona. False for <see cref="PersonaId"/>, true for <see cref="CustomPersona"/>.
		/// </summary>
		public bool UseCustomPersona
		{
			get => _useCustomPersona;
			set => SetProperty(ref _useCustomPersona, value);
		}

		private string? _customPersona;
		/// <summary>
		/// The custom personality prompt to use for the agent, if not null or empty, this will be used instead of <see cref="PersonaId"/>.
		/// </summary>
		public string? CustomPersona
		{
			get => _customPersona;
			set => SetProperty(ref _customPersona, value);
		}

		private Guid? _personaId;
		/// <summary>
		/// The personality ID of the agent. This can be used to influence the behavior and tone of the agent.
		/// The identifier leads to <see cref="Prompting.PromptRegistry.GetPersona(Guid)"/>
		/// </summary>
		public Guid? PersonaId
		{
			get => _personaId;
			set => SetProperty(ref _personaId, value);
		}
	}
}
