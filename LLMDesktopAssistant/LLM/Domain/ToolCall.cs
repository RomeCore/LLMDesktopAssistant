using LLMDesktopAssistant.Tools;
using Material.Icons;
using RCLargeLanguageModels.Tasks;
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
		public required string ToolName { get; init; }

		/// <summary>
		/// Gets or sets the title of the tool being called. Used for display purposes.
		/// </summary>
		public required string? Title { get; init; }

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

		private MaterialIconKind? _statusIcon;
		/// <summary>
		/// The status icon to be displayed. This will be shown next to the main title (that contains tool name).
		/// </summary>
		public MaterialIconKind? StatusIcon
		{
			get => _statusIcon;
			set => SetProperty(ref _statusIcon, value);
		}

		private string? _statusTitle;
		/// <summary>
		/// The title of the status that will be shown next to the main title (that contains tool name).
		/// </summary>
		public string? StatusTitle
		{
			get => _statusTitle;
			set => SetProperty(ref _statusTitle, value);
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

		private TaskCompletionSource<bool>? _userAskCompletionSource;
		/// <summary>
		/// Gets or sets the task completion source for user interaction.
		/// </summary>
		public TaskCompletionSource<bool>? UserAskCompletionSource
		{
			get => _userAskCompletionSource;
			set => SetProperty(ref _userAskCompletionSource, value);
		}

		private ReactiveToolResult? _reactiveToolResult;
		/// <summary>
		/// Gets or sets the reactive tool result. This is used for real-time updates and interactions.
		/// Only avalable during execution.
		/// </summary>
		public ReactiveToolResult? ReactiveToolResult
		{
			get => _reactiveToolResult;
			set => SetProperty(ref _reactiveToolResult, value);
		}

		/// <summary>
		/// Gets or sets the completion token associated with this tool call.
		/// </summary>
		public required CompletionToken CompletionToken { get; init; }

		/// <summary>
		/// Gets a value indicating whether the tool call has been completed (e.g. not processing or pending).
		/// This means this tool call has finished or been loaded from database.
		/// </summary>
		public bool IsCompleted => CompletionToken.IsCompleted;

		public CompletionToken GetAwaiter() => CompletionToken;
	}
}