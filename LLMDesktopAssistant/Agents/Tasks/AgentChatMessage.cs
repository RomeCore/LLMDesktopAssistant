namespace LLMDesktopAssistant.Agents.Tasks
{
	public abstract class AgentChatMessage : NotifyPropertyChanged
	{
		private string? _content;
		/// <summary>
		/// The textual content of the message.
		/// </summary>
		public string? Content
		{
			get => _content;
			set => SetProperty(ref _content, value);
		}
	}
}
