using System.Text.Json.Nodes;

namespace LLMDesktopAssistant.LLM.Domain
{
	/// <summary>
	/// Represents a tool call within an assistant message.
	/// </summary>
	public class ToolCall : NotifyPropertyChanged
	{
		/// <summary>
		/// Gets or sets the name of the tool being called.
		/// </summary>
		public required string Name { get; init; }

		/// <summary>
		/// Gets or sets the ID of the tool call.
		/// </summary>
		public required string Id { get; init; }

		/// <summary>
		/// Gets or sets the arguments passed to the tool. These are typically in JSON format.
		/// </summary>
		public required JsonNode Arguments { get; init; }

		private ToolStatus _status;
		/// <summary>
		/// Gets or sets the status of the tool call.
		/// </summary>
		public ToolStatus Status
		{
			get => _status;
			set => SetProperty(ref _status, value);
		}

		private string? _resultContent = string.Empty;
		/// <summary>
		/// Gets or sets the content of the result returned by the tool.
		/// </summary>
		public string? ResultContent
		{
			get => _resultContent;
			set => SetProperty(ref _resultContent, value);
		}
	}
}