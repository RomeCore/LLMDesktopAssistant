using System.Text.Json.Nodes;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.Tools.Meta
{
	/// <summary>
	/// Defines a scripting engine that can handle meta tools written in a specific language.
	/// Each engine knows how to serialize, deserialize, and create executors for its language.
	/// </summary>
	public interface IMetaToolEngine
	{
		/// <summary>
		/// The scripting language this engine handles.
		/// </summary>
		ScriptLanguageType Language { get; }

		/// <summary>
		/// A descriptor for this engine, providing additional information about serialization and extensions.
		/// </summary>
		IMetaToolEngineDescriptor Descriptor { get; }

		/// <summary>
		/// Creates an executor function for the given meta tool.
		/// The executor is invoked when the LLM calls the tool.
		/// </summary>
		/// <param name="tool">The meta tool to create an executor for.</param>
		/// <returns>A function that executes the tool with the given arguments and context.</returns>
		Func<JsonNode?, ToolExecutionContext, CancellationToken, Task<ReactiveToolResult>> CreateExecutor(MetaToolInfo tool);
	}
}
