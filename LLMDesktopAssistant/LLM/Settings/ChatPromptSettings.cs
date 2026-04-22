using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Settings for system prompts and persona configuration.
	/// </summary>
	public class ChatPromptSettings : NotifyPropertyChanged
	{
		private string? _systemPrompt;
		/// <summary>
		/// The system prompt to use for the chat.
		/// </summary>
		public string? SystemPrompt
		{
			get => _systemPrompt;
			set => SetProperty(ref _systemPrompt, value);
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