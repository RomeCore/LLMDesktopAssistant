using System.Text.Json.Nodes;
using LLMDesktopAssistant.Scripting;

namespace LLMDesktopAssistant.Tools.Meta
{
	/// <summary>
	/// Represents information about a meta tool, including its parsed metadata, source and diagnostics.
	/// </summary>
	public class MetaToolInfo
	{
		/// <summary>
		/// Gets the unique name of the tool.
		/// </summary>
		public required string Name { get; init; }

		/// <summary>
		/// Gets the human-readable title of the tool.
		/// </summary>
		public string Title { get; init; } = string.Empty;

		/// <summary>
		/// Gets the LLM-readable description of the tool.
		/// </summary>
		public string Description { get; init; } = string.Empty;

		/// <summary>
		/// Gets the category of the tool used to group tools in the UI.
		/// </summary>
		public string Category { get; init; } = string.Empty;

		/// <summary>
		/// Gets the behaviours of the tool describing what the tool does.
		/// </summary>
		public ToolBehaviour Behaviours { get; init; } = ToolBehaviour.None;

		/// <summary>
		/// Gets the schema of the arguments that the tool accepts.
		/// </summary>
		public JsonObject? ArgumentSchema { get; init; }

		/// <summary>
		/// Gets the language in which the tool's execution code is written.
		/// </summary>
		public ScriptLanguageType ScriptLanguage { get; init; } = ScriptLanguageType.Unknown;

		/// <summary>
		/// Gets the actual code that the tool executes.
		/// </summary>
		public string ExecutionCode { get; init; } = string.Empty;

		/// <summary>
		/// Gets the source of the tool file.
		/// </summary>
		public MetaToolSource Source { get; init; }

		/// <summary>
		/// Gets the absolute path to the tool file, if applicable.
		/// </summary>
		public string? Path { get; init; }

		/// <summary>
		/// Gets the diagnostic containing warnings and errors that occurred during tool parsing.
		/// </summary>
		public MetaToolDiagnostic? Diagnostic { get; init; }

		/// <summary>
		/// Gets a value indicating whether the tool is enabled. Defaults to <see langword="null"/>.
		/// </summary>
		public bool? Enabled { get; init; } = null;

		/// <summary>
		/// Gets the default approval level of the tool.
		/// </summary>
		public ToolApprovalLevel? ApprovalLevel { get; init; }
	}
}
