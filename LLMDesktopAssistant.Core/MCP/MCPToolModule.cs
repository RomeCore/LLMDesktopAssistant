using LLMDesktopAssistant.Core.ToolModules;
using LLMDesktopAssistant.Core.Utils;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using RCLargeLanguageModels.Tools;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace LLMDesktopAssistant.Core.MCP
{
	/// <summary>
	/// A tool module that integrates with an MCP server to retrieve and execute tools defined on the server.
	/// It connects to the MCP server, lists available tools, converts them into a format compatible with the LLM assistant,
	/// and provides execution logic to call the tools on the server when invoked by the assistant.
	/// </summary>
	public partial class MCPToolModule : ToolModule, IAsyncDisposable
	{
		private readonly MCPConnection _connection;
		private readonly IAsyncDisposable _listChangedSub;

		private MCPToolModule(MCPConnection connection, IEnumerable<ToolInfo> tools)
		{
			_connection = connection;
			_listChangedSub = _connection.Client.RegisterNotificationHandler("tools/list_changed", ToolsChanged);
			ReplaceTools(tools);
		}

		private async ValueTask ToolsChanged(JsonRpcNotification notification, CancellationToken cancellationToken)
		{
			var tools = await ListToolsAsync(_connection, cancellationToken);
			ReplaceTools(tools);
		}

		public async ValueTask DisposeAsync()
		{
			await _listChangedSub.DisposeAsync();
		}

		/// <summary>
		/// Asynchronously creates an instance of the <see cref="MCPToolModule"/> by connecting to the
		/// MCP server using the provided connection, retrieving the list of tools from the server,
		/// converting them into the appropriate format, and initializing the module with these tools.
		/// </summary>
		/// <param name="connection"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public static async Task<MCPToolModule> CreateAsync(MCPConnection connection,
			CancellationToken cancellationToken = default)
		{
			return new MCPToolModule(connection, await ListToolsAsync(connection, cancellationToken));
		}

		private static async Task<IEnumerable<ToolInfo>> ListToolsAsync(MCPConnection connection, CancellationToken cancellationToken = default)
		{
			var tools = await connection.Client.ListToolsAsync(cancellationToken: cancellationToken);
			return tools.Select(t => ConvertTool(connection, t));
		}

		private static readonly Regex _toolNameInvalidFilterRegex = ToolNameFilterRegex();
		[GeneratedRegex(@"[^a-zA-Z0-9_-]+", RegexOptions.Compiled)]
		private static partial Regex ToolNameFilterRegex();

		private static string ConvertToolPrefix(string serverName)
		{
			serverName = serverName.Replace(" ", "_").ToLower();
			serverName = _toolNameInvalidFilterRegex.Replace(serverName, ""); // Remove invalid characters for tool names
			return serverName;
		}

		private static ToolInfo ConvertTool(MCPConnection connection, McpClientTool mcpTool)
		{
			var argSchema = JsonObject.Create(mcpTool.JsonSchema)
				?? throw new InvalidOperationException($"Unsupported JSON schema type: {mcpTool.JsonSchema.ValueKind}");

			// Use connection name as prefix
			var name = ConvertToolPrefix(connection.Info.Name) + '-' + mcpTool.Name;

			var toolInfo = new ToolInfo
			{
				Tool = new FunctionTool(name, mcpTool.Description, argSchema, CreateFunctionExecutor(connection, mcpTool)),
				Source = ToolSource.MCP,
				DisplayName = mcpTool.Title ?? mcpTool.Name,
				Category = connection.Info.Name
			};
			return toolInfo;
		}

		private static Func<JsonNode, CancellationToken, Task<ToolResult>> CreateFunctionExecutor(
			MCPConnection connection, McpClientTool mcpTool)
		{
			return async (args, cancellationToken) =>
			{
				try
				{
					var dictArgs = args.Deserialize<Dictionary<string, object?>>();
					var result = await mcpTool.CallAsync(dictArgs, cancellationToken: cancellationToken);

					var contents = string.Join("\n\n", result.Content.Select(c =>
					{
						if (c is TextContentBlock textCb)
							return textCb.Text;
						else if (c is ToolResultContentBlock trCb)
							return trCb.StructuredContent?.ToJsonString() ?? string.Empty;
						return "";
					}));

					if (result.IsError == true)
					{
						if (string.IsNullOrWhiteSpace(contents))
							contents = "Tool executed with an error.";
						return new ToolResult(ToolResultStatus.Error, contents);
					}
					else
					{
						if (string.IsNullOrWhiteSpace(contents))
							contents = "Tool executed successfully with no output.";
						return new ToolResult(contents);
					}
				}
				catch (AggregateException aex) when (aex.InnerExceptions.Any(e => e is OperationCanceledException))
				{
					return new ToolResult(ToolResultStatus.Cancelled, "Tool execution was cancelled.");
				}
				catch (OperationCanceledException)
				{
					return new ToolResult(ToolResultStatus.Cancelled, "Tool execution was cancelled.");
				}
				catch (Exception ex)
				{
					return new ToolResult(ToolResultStatus.Error, $"Error calling MCP '{connection.Info.Name}' tool: {ex.Message}");
				}
			};
		}
	}
}