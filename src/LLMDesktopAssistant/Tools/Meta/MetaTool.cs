using System.Text.Json.Nodes;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Tools.Meta
{
	public class MetaTool
	{
		/// <summary>
		/// The name of the tool. This is used to identify the tool in the system.
		/// </summary>
		public required string Name { get; init; }

		/// <summary>
		/// Whether or not the tool is local to the current working directory. If false, the tool is located in <see cref="Directories.Metatools"/>.
		/// </summary>
		public required bool IsLocal { get; init; }

		/// <summary>
		/// The title of the tool. This is used to display the tool in the UI.
		/// </summary>
		public required string Title { get; init; }

		/// <summary>
		/// The LLM-readable description of the tool.
		/// </summary>
		public required string Description { get; init; }

		/// <summary>
		/// The category of the tool. This is used to group tools in the UI.
		/// </summary>
		public required string Category { get; init; }

		/// <summary>
		/// The default approval level of the tool.
		/// </summary>
		public required ToolApprovalLevel ApprovalLevel { get; init; }

		/// <summary>
		/// The behaviour of the tool. This allows to approve or deny the tool based on certain policies.
		/// </summary>
		public required ToolBehaviour Behaviours { get; init; }

		/// <summary>
		/// The schema of the arguments that the tool accepts.
		/// </summary>
		public required JsonObject? ArgumentSchema { get; init; }

		/// <summary>
		/// The language in which the tool's execution code is written.
		/// </summary>
		public required ScriptLanguageType ScriptLanguage { get; init; }

		/// <summary>
		/// The actual code that the tool executes. This is written in the language specified by <see cref="ScriptLanguage"/>.
		/// </summary>
		public required string ExecutionCode { get; init; }
	}
}