using System.Collections.Concurrent;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Tools.Meta;
using LLMDesktopAssistant.Utils;
using Microsoft.CodeAnalysis.CSharp.Scripting;
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
		private static readonly ScriptOptions Options = ScriptOptions.Default
			.AddReferences(GetLoadedAssemblies())
			.WithImports(
				"System",
				"System.Collections.Generic",
				"System.Linq",
				"System.Net.Http",
				"System.Text",
				"System.Text.Json",
				"System.Text.Json.Nodes",
				"System.Threading",
				"System.Threading.Tasks",
				"LLMDesktopAssistant",
				"LLMDesktopAssistant.Tools",
				"LLMDesktopAssistant.Tools.Meta",
				"LLMDesktopAssistant.Scripting",
				"LLMDesktopAssistant.Utils");

		private readonly ConcurrentDictionary<string, Script<object?>> _compiledScripts = new();

		/// <inheritdoc/>
		public ScriptLanguageType Language => ScriptLanguageType.CSharpScript;

		/// <inheritdoc/>
		public IMetaToolEngineDescriptor Descriptor { get; } = new CSharpScriptMetaToolEngineDescriptor();

		/// <inheritdoc/>
		public Func<JsonNode?, ToolExecutionContext, CancellationToken, Task<ReactiveToolResult>> CreateExecutor(MetaTool tool)
		{
			return (JsonNode? args, ToolExecutionContext context, CancellationToken cancellationToken) =>
			{
				var reactiveResult = new ReactiveToolResult();

				_ = Task.Run(async () =>
				{
					try
					{
						var script = GetOrCreateScript(tool.ExecutionCode);
						var globals = new CSharpScriptGlobals
						{
							ToolArgs = args,
							Context = context,
							Result = new CSharpScriptToolResult(reactiveResult)
						};

						var state = await script.RunAsync(globals: globals, cancellationToken: cancellationToken);
						var returnValue = state.ReturnValue;

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

		private Script<object?> GetOrCreateScript(string code)
		{
			var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(code)));
			return _compiledScripts.GetOrAdd(key, _ => CSharpScript.Create<object?>(code, Options, typeof(CSharpScriptGlobals)));
		}

		private static IReadOnlyList<Assembly> GetLoadedAssemblies()
		{
			try
			{
				return ReflectionUtility.AllAssemblies
					.Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
					.ToArray();
			}
			catch (InvalidOperationException)
			{
				return AppDomain.CurrentDomain.GetAssemblies()
					.Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
					.ToArray();
			}
		}
	}
}
