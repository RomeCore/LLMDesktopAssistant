using System.Text.Json.Nodes;

namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// The class that provides initialization information for a <see cref="ToolInfo"/>.
	/// </summary>
	public class ToolInitializationInfo : ToolInfo
	{
		/// <summary>
		/// The executor delegate that will be invoked when the tool is executed. This is a required property.
		/// </summary>
		public new required Delegate Executor { get; init; }

		/// <summary>
		/// The streaming analyzer delegate that will be invoked on every update of tool call arguments.
		/// </summary>
		public new Delegate? StreamingAnalyzer { get; init; }

		/// <summary>
		/// The preview executor delegate that will be invoked before main executor and specifier analyzer.
		/// Used for determine behaviour of the tool and short-circuiting when arguments are not valid semantically
		/// (e.g. file for deletion not exists).
		/// </summary>
		public new Delegate? PreviewExecutor { get; init; }

		/// <summary>
		/// The specifier analyzer delegate used to check tool call arguments against the specifier.
		/// Used for advanced policy checks (e.g. check against allowed command masks for shell execution).
		/// </summary>
		public new Delegate? SpecifierAnalyzer { get; init; }

		/// <summary>
		/// The action to modify arguments before they are passed to the <see cref="ToolInfo"/>.
		/// </summary>
		public Action<JsonObject>? ModifyArgumentSchema { get; init; } = null;
	}
}