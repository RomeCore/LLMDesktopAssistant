using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Describes an agent prompt settings.
	/// Contains the system prompt, nickname, persona and other settings.
	/// </summary>
	public class AgentPromptSettings : NotifyPropertyChanged
	{
		private string? _systemPrompt;
		/// <summary>
		/// The system prompt to use for the agent.
		/// </summary>
		public string? SystemPrompt
		{
			get => _systemPrompt;
			set => SetProperty(ref _systemPrompt, value);
		}

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

		/// <summary>
		/// Alias for <see cref="Nickname"/> — the agent's own nickname (how it calls itself).
		/// Used for backward compatibility with <see cref="ChatPromptSettings"/> bindings.
		/// </summary>
		public string? AssistantNickname
		{
			get => Nickname;
			set => Nickname = value;
		}

		private string? _userNickname;
		/// <summary>
		/// The nickname the agent should use to refer to the user.
		/// </summary>
		public string? UserNickname
		{
			get => _userNickname;
			set => SetProperty(ref _userNickname, value);
		}

		private readonly RangeObservableCollection<Guid> _promptComponents = [];
		/// <summary>
		/// The collection of prompt components IDs that should be appended to the system message in addition to the <see cref="SystemPrompt"/>.
		/// The identifiers leads to <see cref="Prompting.PromptRegistry.GetComponent(Guid)"/>
		/// </summary>
		public ICollection<Guid> PromptComponents
		{
			get => _promptComponents;
			set => _promptComponents.Reset(value);
		}

		private bool _useCustomPersona = false;
		/// <summary>
		/// Whether to use a custom persona for the chat. False for <see cref="PersonaId"/>, true for <see cref="CustomPersona"/>.
		/// </summary>
		public bool UseCustomPersona
		{
			get => _useCustomPersona;
			set => SetProperty(ref _useCustomPersona, value);
		}

		private string? _customPersona;
		/// <summary>
		/// The custom personality prompt to use for chat, if not null or empty, this will be used instead of <see cref="PersonaId"/>.
		/// </summary>
		public string? CustomPersona
		{
			get => _customPersona;
			set => SetProperty(ref _customPersona, value);
		}

		private Guid? _personaId;
		/// <summary>
		/// The personality ID of the chatbot. This can be used to influence the behavior and tone of the chatbot.
		/// The identifier leads to <see cref="Prompting.PromptRegistry.GetPersona(Guid)"/>
		/// </summary>
		public Guid? PersonaId
		{
			get => _personaId;
			set => SetProperty(ref _personaId, value);
		}
	}
}
