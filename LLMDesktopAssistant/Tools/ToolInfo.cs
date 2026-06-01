using System.Text.Json.Nodes;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Tools
{
	/// <summary>
	/// The class that provides information about a tool.
	/// </summary>
	public class ToolInfo
	{
		/// <summary>
		/// Gets or sets the name of the tool. This is a required property.
		/// </summary>
		public required string Name { get; init; }

		/// <summary>
		/// Gets or sets a function that returns the description of the tool. This is useful for dynamic descriptions based on runtime conditions.
		/// </summary>
		public required Func<string> DescriptionGetter { get; init; } = () => "";

		/// <summary>
		/// Gets or sets a JSON object that defines the schema of the arguments for the tool.
		/// </summary>
		public required JsonObject ArgumentSchema { get; init; }

		/// <summary>
		/// Gets or sets a JSON object that defines the schema of the structured output for the tool.
		/// Can be null if tool does not produces structured output.
		/// </summary>
		public JsonObject? OutputSchema { get; init; }

		/// <summary>
		/// Gets a <see cref="FunctionTool"/> instance that represents this tool.
		/// Used for API registration purposes.
		/// </summary>
		public FunctionTool Tool => new(Name, DescriptionGetter(), ArgumentSchema,
			(_, _) => throw new NotSupportedException("This function tool is not meant to be executed directly. Use Executor instead."));

		/// <summary>
		/// Gets a <see cref="FunctionTool"/> instance that represents this tool.
		/// Can be executed also (instead of <see cref="Tool"/>), usable for RCLLM agentic API.
		/// </summary>
		public FunctionTool GetExecutableTool(ToolExecutionContext ctx) => new(Name, DescriptionGetter(), ArgumentSchema,
			async (args, ct) =>
			{
				try
				{
					var result = await Executor.Invoke(args, ctx, ct);
					var success = await result.Completion;
					var content = result.ResultContent;
					if (string.IsNullOrEmpty(content))
						content = "Tool did not returned any result.";
					return new ToolResult(success ? ToolResultStatus.Success : ToolResultStatus.Error, content);
				}
				catch (Exception ex)
				{
					return new ToolResult(ToolResultStatus.Error, $"Error occured while executing tool: " + ex.Message);
				}
			});

		/// <summary>
		/// Gets or sets a pre-execution function for the tool. This function is responsible for performing any necessary checks or preparations before executing the tool.
		/// </summary>
		public Func<JsonNode, ToolExecutionContext, CancellationToken, Task<PreviewToolExecutionResult>>? PreviewExecutor { get; init; }

		/// <summary>
		/// Gets or sets the executor function for the tool. This function is responsible for executing the tool with the provided arguments and context.
		/// </summary>
		public required Func<JsonNode, ToolExecutionContext, CancellationToken, Task<ReactiveToolResult>> Executor { get; init; }

		/// <summary>
		/// Gets or sets the user-friendly display name of the tool. If not set, the tool's name will be used as the display name.
		/// </summary>
		public string? DisplayName { get; init; }

		/// <summary>
		/// Gets or sets the category of the tool. Defaults to "general".
		/// </summary>
		public string Category { get; init; } = "general";

		/// <summary>
		/// Gets or sets the source of the tool. Defaults to "native".
		/// </summary>
		public ToolSource Source { get; init; } = ToolSource.Native;

		/// <summary>
		/// Gets or sets a value indicating whether the tool is enabled. Defaults to true.
		/// </summary>
		public bool Enabled { get; init; } = true;

		/// <summary>
		/// Gets or sets a value indicating whether the tool requires user confirmation before execution.
		/// </summary>
		public bool AskForConfirmation { get; init; } = false;

		/// <summary>
		/// Creates a new instance of the <see cref="ToolInfo"/> class with the specified executor and initialization information.
		/// </summary>
		/// <param name="executor">The delegate representing the executor function for the tool.</param>
		/// <param name="previewExecutor">The delegate representing the pre-execution function for the tool. This can be null if no pre-execution is required.</param>
		/// <param name="info">The initialization information for the tool. This includes various properties such as name, description, and category.</param>
		/// <returns>The newly created <see cref="ToolInfo"/> instance.</returns>
		/// <exception cref="InvalidOperationException">Thrown when the description getter is not provided in the initialization information.</exception>
		public static ToolInfo Create(Delegate executor, Delegate? previewExecutor, ToolInitializationInfo info)
		{
			ToolName.EnsureValid(info.Name);

			if (info.DescriptionGetter == null)
				throw new InvalidOperationException($"Description of the {nameof(ToolInitializationInfo)} must be set before tool creation.");

			var (argSchema, _executor) = ToolExecutorCreator.Create(executor);
			var _previewExecutor = ToolPreviewExecutorCreator.Create(previewExecutor);

			return new ToolInfo
			{
				Name = info.Name,
				DescriptionGetter = info.DescriptionGetter,
				ArgumentSchema = argSchema,
				Executor = _executor,
				PreviewExecutor = _previewExecutor,
				DisplayName = info.DisplayName,
				Category = info.Category,
				AskForConfirmation = info.AskForConfirmation,
				Enabled = info.Enabled,
				Source = info.Source
			};
		}
	}
}