using System.Text.Json;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Tools.Meta;
using LLMDesktopAssistant.Utils;
using Microsoft.CodeAnalysis.Scripting;

namespace LLMDesktopAssistant.Scripting.CSX
{
	/// <summary>
	/// C# Script implementation of <see cref="IMetaToolEngine"/>.
	/// Handles meta tools written in C# (.csx) with YAML frontmatter in <c>/* ... */</c> comment blocks.
	/// Scripts are compiled with Roslyn and have full access to all loaded assemblies.
	/// </summary>
	[ChatService(typeof(IMetaToolEngine))]
	public class CSharpScriptMetaToolEngine : IMetaToolEngine
	{
		private readonly CSharpScriptService _scriptService;

		/// <inheritdoc/>
		public ScriptLanguageType Language => ScriptLanguageType.CSharpScript;

		/// <inheritdoc/>
		public IMetaToolEngineDescriptor Descriptor { get; } = new CSharpScriptMetaToolEngineDescriptor();

		/// <summary>
		/// Initializes a new instance of the <see cref="CSharpScriptMetaToolEngine"/> class.
		/// </summary>
		/// <param name="scriptService">The service used to run C# scripts.</param>
		public CSharpScriptMetaToolEngine(CSharpScriptService scriptService)
		{
			_scriptService = scriptService;
		}

		/// <inheritdoc/>
		public Func<JsonNode?, ToolExecutionContext, CancellationToken, Task<ReactiveToolResult>> CreateExecutor(MetaToolInfo tool)
		{
			return (JsonNode? args, ToolExecutionContext context, CancellationToken cancellationToken) =>
			{
				var reactiveResult = new ReactiveToolResult();

				_ = Task.Run(async () =>
				{
					try
					{
						var workdir = context.Chat.Services.GetService<IWorkingDirectoryAccessService>()?.GetWorkingDirectory();

						var globals = new CSharpScriptGlobals
						{
							ToolArgs = args,
							Context = context,
							Result = new CSharpScriptToolResult(reactiveResult),
							Workdir = workdir ?? Directories.DefaultWorkingDirectory,
						};

						var returnValue = await _scriptService.RunAsync(tool.ExecutionCode, globals, cancellationToken);

						if (reactiveResult.StructuredResult == null && returnValue != null)
						{
							try
							{
								reactiveResult.StructuredResult = JsonSerializer.SerializeToNode(returnValue);
							}
							catch (Exception ex)
							{
								reactiveResult.ResultContentLines.Add("Failed to serialize return value: " + ex.Message);
							}
						}

						reactiveResult.ResultContentLines.Add("Script returned: " + returnValue);
						reactiveResult.TryCompleteWithSuccess();
					}
					catch (CompilationErrorException cex)
					{
						reactiveResult.ResultContentLines.Add("Compilation errors:");
						foreach (var diagnostic in cex.Diagnostics)
							reactiveResult.ResultContentLines.Add(diagnostic.ToString());
						reactiveResult.TryCompleteWithError();
					}
					catch (Exception ex)
					{
						reactiveResult.ResultContentLines.Add("Caught error: " + ex.Message);
						reactiveResult.TryCompleteWithError();
					}
				}, CancellationToken.None);

				return Task.FromResult(reactiveResult);
			};
		}
	}
}
