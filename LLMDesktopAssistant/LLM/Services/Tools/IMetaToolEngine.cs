using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Tools;
using System.Text.Json.Nodes;

namespace LLMDesktopAssistant.LLM.Services.Tools
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
		/// The file extension for this engine's scripts (e.g., ".lua", ".py").
		/// </summary>
		string FileExtension { get; }

		/// <summary>
		/// Example JSON arguments schema to show LLM how to call a meta tool.
		/// Used in tool descriptions for <c>metatools-create_or_update</c>.
		/// </summary>
		string ExampleArgs { get; }

		/// <summary>
		/// Example execution code to show LLM the syntax for this language.
		/// Used in tool descriptions for <c>metatools-create_or_update</c>.
		/// </summary>
		string ExampleCode { get; }

		/// <summary>
		/// Deserializes a meta tool from file content.
		/// </summary>
		/// <param name="fileContent">The raw content of the tool file.</param>
		/// <param name="name">The name of the tool (derived from file name).</param>
		/// <returns>The deserialized meta tool.</returns>
		MetaTool Deserialize(string fileContent, string name);

		/// <summary>
		/// Serializes a meta tool to file content for storage.
		/// </summary>
		/// <param name="tool">The meta tool to serialize.</param>
		/// <returns>The file content to write.</returns>
		string Serialize(MetaTool tool);

		/// <summary>
		/// Creates an executor function for the given meta tool.
		/// The executor is invoked when the LLM calls the tool.
		/// </summary>
		/// <param name="tool">The meta tool to create an executor for.</param>
		/// <returns>A function that executes the tool with the given arguments and context.</returns>
		Func<JsonNode, ToolExecutionContext, CancellationToken, Task<ReactiveToolResult>> CreateExecutor(MetaTool tool);
	}
}
