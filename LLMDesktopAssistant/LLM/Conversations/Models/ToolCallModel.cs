using LiteDB;

namespace LLMDesktopAssistant.LLM.Conversations.Models
{
	/// <summary>
	/// Represents a model for a tool call inside the database.
	/// </summary>
	public sealed class ToolCallModel
	{
		/// <summary>
		/// The unique identifier for the tool call.
		/// </summary>
		[BsonId]
		public int Id { get; set; }

		/// <summary>
		/// Gets or sets the message ID that this tool call belongs to. Valid when the message is assistant message.
		/// </summary>
		public int MessageId { get; set; }

		/// <summary>
		/// Gets or sets the tool type that this tool call is associated to.
		/// </summary>
		public ToolType ToolType { get; set; } = ToolType.Function;

		/// <summary>
		/// Gets or sets the tool name that being/been called.
		/// </summary>
		public string ToolName { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the LLM API-specific tool call id that used by LLM to correctly bind each tool call to the tool message.
		/// </summary>
		public string ToolCallId { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the function call arguments, this is JSON content serialized to string.
		/// </summary>
		public string FunctionArguments { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the result status of the tool call, valid when tool call is executed.
		/// </summary>
		public ToolStatus? ResultStatus { get; set; }

		/// <summary>
		/// Gets or sets the result content of the tool call, valid when tool call is executed.
		/// </summary>
		public string? ResultContent { get; set; }

		/// <summary>
		/// Gets or sets the MarkDown-formatted result content of the tool call.
		/// Used for display purposes, can be null when normal content should be used.
		/// </summary>
		public string? FormattedResultContent { get; set; }
	}
}