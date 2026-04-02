using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.ToolModules;
using LLMDesktopAssistant.Utils;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.MCP
{
	/// <summary>
	/// A tool module that integrates with an MCP server to retrieve and execute tools defined on the server.
	/// It connects to the MCP server, lists available tools, converts them into a format compatible with the LLM assistant,
	/// and provides execution logic to call the tools on the server when invoked by the assistant.
	/// </summary>
	public class MCPToolModule : ToolModule
	{
		private readonly MCPConnection _connection;
		private readonly ImmutableList<ToolInfo> _tools;

		private MCPToolModule(MCPConnection connection, ImmutableList<ToolInfo> tools)
		{
			_connection = connection;
			_tools = tools;

			foreach (var tool in tools)
				AddTool(tool);
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
			var tools = await connection.Client.ListToolsAsync(cancellationToken: cancellationToken);
			var convertedTools = tools.Select(t => ConvertTool(connection, t)).ToImmutableList();
			return new MCPToolModule(connection, convertedTools);
		}

		private static ToolInfo ConvertTool(MCPConnection connection, McpClientTool mcpTool)
		{
			var argSchema = JsonObject.Create(mcpTool.JsonSchema)
				?? throw new InvalidOperationException($"Unsupported JSON schema type: {mcpTool.JsonSchema.ValueKind}");
			// Use connection name as prefix
			var name = connection.Info.Name + '-' + mcpTool.Name;
			var toolInfo = new ToolInfo
			{
				Tool = new FunctionTool(name, mcpTool.Description, argSchema, CreateFunctionExecutor(mcpTool)),
				DisplayName = $"[{connection.Info.Name}] {mcpTool.Name}",
				Category = connection.Info.Name
			};
			return toolInfo;
		}

		private static Func<JsonNode, CancellationToken, Task<ToolResult>> CreateFunctionExecutor(McpClientTool mcpTool)
		{
			return async (args, cancellationToken) =>
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
			};
		}
	}
}