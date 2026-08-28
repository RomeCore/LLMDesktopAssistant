using LLMDesktopAssistant.Prompting;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Represents the persona group of an agent prompt: the agent's nickname and the persona
	/// selection (a registered persona or a custom persona text).
	/// </summary>
	public class PersonaSettings : PromptPartKeyedSelection<Guid>
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
		/// Whether to use a custom persona.
		/// </summary>
		public bool UseCustomPersona
		{
			get => _useCustomPersona;
			set => SetProperty(ref _useCustomPersona, value);
		}

		private string? _customPersona;
		/// <summary>
		/// The custom personality prompt to use for the agent.
		/// </summary>
		public string? CustomPersona
		{
			get => _customPersona;
			set => SetProperty(ref _customPersona, value);
		}
	}
}
