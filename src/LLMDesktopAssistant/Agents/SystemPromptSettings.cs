using LLMDesktopAssistant.Prompting;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// Represents the specialization group of an agent prompt: the selection between a
	/// registered specialization and a custom specialization text.
	/// </summary>
	public class SystemPromptSettings : PromptPartKeyedSelection<Guid>
	{
		private bool _useCustomSystemPrompt = true;
		/// <summary>
		/// Whether to use a custom system prompt.
		/// </summary>
		public bool UseCustomSystemPrompt
		{
			get => _useCustomSystemPrompt;
			set => SetProperty(ref _useCustomSystemPrompt, value);
		}

		private string? _customSystemPrompt;
		/// <summary>
		/// The custom system prompt to use.
		/// </summary>
		public string? CustomSystemPrompt
		{
			get => _customSystemPrompt;
			set => SetProperty(ref _customSystemPrompt, value);
		}
	}
}
