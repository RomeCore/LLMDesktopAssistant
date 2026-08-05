using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.Desktop.Execution;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Tools.Meta;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Desktop.Scripting.Python
{
	/// <summary>
	/// Python implementation of <see cref="IMetaToolEngine"/>.
	/// Handles meta tools written in Python with YAML frontmatter in `"""` docstring blocks.
	/// Requires Python runtime and optional virtual environment.
	/// Only available on Desktop platform.
	/// </summary>
	[Service(typeof(IMetaToolEngine))]
	public class PythonMetaToolEngine : IMetaToolEngine
	{
		private readonly IProcessLauncher _processLauncher;
		private readonly PythonHelperService _pythonHelperService;

		public ScriptLanguageType Language => ScriptLanguageType.Python;

		public IMetaToolEngineDescriptor Descriptor { get; } = new PythonMetaToolEngineDescriptor();

		public PythonMetaToolEngine(IProcessLauncher processLauncher, PythonHelperService pythonHelperService)
		{
			_processLauncher = processLauncher;
			_pythonHelperService = pythonHelperService;
		}

		public Func<JsonNode?, ToolExecutionContext, CancellationToken, Task<ReactiveToolResult>> CreateExecutor(MetaTool tool)
		{
			return async (JsonNode? args, ToolExecutionContext context, CancellationToken cancellationToken) =>
			{
				try
				{
					string pythonCode = $"""
						import sys
						sys.stdout.reconfigure(encoding="utf-8")
						sys.stderr.reconfigure(encoding="utf-8")
						tool_args = {SerializeNodeToPython(args)}
						{tool.ExecutionCode}
						""";

					var chat = context.Chat;
					var workDir = chat.Settings.Environment.GetEffectiveWorkingDirectories().GetWorkingDirectory();

					var tempPyFile = Path.GetFullPath(Path.Combine(workDir, $"{Guid.NewGuid()}.py"));
					File.WriteAllText(tempPyFile, pythonCode);

					ProcessDescriptor? process = null;
					try
					{
						process = _processLauncher.Launch(_pythonHelperService.CreateLaunchParameters(
							chat.Settings.Environment, $"python \"{tempPyFile}\"", "Python Meta", false, true), cancellationToken);

						int exitCode = await process;
						return ReactiveToolResult.Create(exitCode == 0, process.Output + $"\nProcess exited with code {exitCode}. Check terminal output above for details.");
					}
					catch (Exception ex)
					{
						if (process != null)
							return ReactiveToolResult.CreateError(process.Output + $"\nProcess finished with error: {ex.Message}");
						else
							return ReactiveToolResult.CreateError(ex.Message);
					}
					finally
					{
						File.Delete(tempPyFile);
					}
				}
				catch (Exception ex)
				{
					return ReactiveToolResult.CreateError($"Python execution error: {ex.Message}");
				}
			};
		}

		private static string SerializeNodeToPython(JsonNode? node)
		{
			if (node == null)
				return "None";

			return node switch
			{
				JsonValue value => SerializeValueToPython(value),
				JsonObject obj => SerializeObjectToPython(obj),
				JsonArray arr => SerializeArrayToPython(arr),
				_ => throw new NotSupportedException($"Unsupported node type: {node.GetType()}")
			};
		}

		private static string SerializeValueToPython(JsonValue value)
		{
			switch (value.GetValueKind())
			{
				case JsonValueKind.Null:
					return "None";
				case JsonValueKind.True:
					return "True";
				case JsonValueKind.False:
					return "False";
				default:
					return value.ToJsonString();
			}
		}

		private static string SerializeObjectToPython(JsonObject obj)
		{
			var parts = new List<string>();

			foreach (var kvp in obj)
			{
				string key = SerializeValueToPython(JsonValue.Create(kvp.Key));
				string value = SerializeNodeToPython(kvp.Value);
				parts.Add($"{key}: {value}");
			}

			return "{" + string.Join(", ", parts) + "}";
		}

		private static string SerializeArrayToPython(JsonArray arr)
		{
			var items = new List<string>();

			foreach (var item in arr)
			{
				items.Add(SerializeNodeToPython(item));
			}

			return "[" + string.Join(", ", items) + "]";
		}
	}
}
