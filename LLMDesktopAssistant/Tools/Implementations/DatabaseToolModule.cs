using System.ComponentModel;
using System.Text;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.Data.Connectors;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Tools.Meta;
using Material.Icons;

namespace LLMDesktopAssistant.Tools.Implementations
{
	/// <summary>
	/// Tool module providing database interaction tools: listing configured connections,
	/// connecting to a database, inspecting its schema and executing SQL statements.
	/// </summary>
	[ToolModule]
	public class DatabaseToolModule : ToolModule
	{
		private static readonly HashSet<string> ReadKeywords = new(StringComparer.OrdinalIgnoreCase)
		{
			"SELECT", "WITH", "PRAGMA", "EXPLAIN", "SHOW", "DESCRIBE"
		};

		private readonly Chat _chat;
		private readonly IDatabaseConnectionManager _connectionManager;

		/// <summary>
		/// Initializes a new instance of the <see cref="DatabaseToolModule"/> class.
		/// </summary>
		/// <param name="chat">The chat whose settings contain the database connections.</param>
		/// <param name="connectionManager">The manager of the active database connection of the chat.</param>
		public DatabaseToolModule(Chat chat, IDatabaseConnectionManager connectionManager)
		{
			_chat = chat;
			_connectionManager = connectionManager;

			AddTool(ListAsync, new ToolInitializationInfo
			{
				Name = "db-list",
				IsFixed = true,
				Description = """
					Lists the database connections configured in the chat settings (named connections and the custom one) and the currently active session connection.
					""",
				DefaultExpectedBehaviour = ToolBehaviour.None,
				Category = "database"
			});

			AddTool(ConnectAsync, null, ConnectPreview, new ToolInitializationInfo
			{
				Name = "db-switch",
				IsFixed = true,
				Description = """
					Activates a database connection for the current chat session: a named connection from the chat settings (pass its name, see db-list) or a custom database by a raw connection string with the specified connector type. Only switches the active connection string; the real connection is opened lazily on the first use (db-schema or db-execute).
					""",
				DefaultExpectedBehaviour = ToolBehaviour.DatabaseCustomConnect,
				Category = "database",
				ModifyArgumentSchema = schema =>
				{
					var properties = schema["properties"]!;

					var connectorType = properties["connectorType"]!;
					connectorType["enum"] = new JsonArray(_connectionManager.SupportedConnectors
						.Select(v => JsonValue.Create(v.ToString().ToLower())).ToArray());
				}
			});

			AddTool(GetSchemaAsync, new ToolInitializationInfo
			{
				Name = "db-schema",
				IsFixed = true,
				Description = """
					Shows the schema of the active database: tables, views and their columns.
					Call db-connect first if no connection is active.
					""",
				DefaultExpectedBehaviour = ToolBehaviour.DatabaseRead,
				Category = "database"
			});

			AddTool(ExecuteAsync, null, ExecutePreview, new ToolInitializationInfo
			{
				Name = "db-execute",
				IsFixed = false,
				Description = """
					Executes a SQL statement against the active database and returns the result.
					SELECT-like statements return a markdown table of rows; modifying statements (INSERT, UPDATE, DELETE, DDL) return the number of affected rows.
					""",
				DefaultExpectedBehaviour = ToolBehaviour.DatabaseRead | ToolBehaviour.DatabaseChange,
				Category = "database"
			});
		}

		private Task ListAsync(ReactiveToolResult result, CancellationToken cancellationToken = default)
		{
			var settings = _chat.Settings.Databases.GetEffectiveDatabaseConnection();
			var sb = new StringBuilder();

			sb.AppendLine("### Named connections");
			var items = settings.Items.Where(i => i.IsEnabled).ToList();
			if (items.Count == 0)
				sb.AppendLine("None configured.");
			else
			{
				foreach (var item in items)
					sb.AppendLine($"- **{item.Name}** ({item.ConnectorType}, {(item.IsActive ? "active" : "inactive")})");
			}

			sb.AppendLine();
			sb.AppendLine("### Custom connection");
			sb.AppendLine(settings.IsCustomActive && !string.IsNullOrEmpty(settings.CustomConnectionString)
				? $"- **custom** ({settings.CustomConnectorType}, active)"
				: "- Not configured or inactive.");

			result.StatusIcon = MaterialIconKind.Database;
			result.UseMarkdown = true;
			result.ResultContent = sb.ToString();
			result.CompleteWithSuccess();
			return Task.CompletedTask;
		}

		private PreviewToolExecutionResult? ConnectPreview(string nameOrConnectionString)
		{
			var settings = _chat.Settings.Databases.GetEffectiveDatabaseConnection();
			bool isNamed = settings.Items.Any(i => i.IsEnabled &&
				string.Equals(i.Name, nameOrConnectionString, StringComparison.OrdinalIgnoreCase));

			return new PreviewToolExecutionResult
			{
				StatusTitle = $"**{nameOrConnectionString}**",
				ExpectedBehaviour = isNamed ? ToolBehaviour.None : ToolBehaviour.DatabaseCustomConnect
			};
		}

		private Task ConnectAsync(
			[Description("The name of the configured database connection (see db-list) or a raw connection string")] string nameOrConnectionString,
			ReactiveToolResult result,
			[Description("The connector type for a custom connection string. Ignored when connecting to a named connection.")] string? connectorType = null)
		{
			result.StatusIcon = MaterialIconKind.DatabaseArrowRight;
			result.StatusTitle = $"**{nameOrConnectionString}**";

			try
			{
				var parsedConnectorType = connectorType != null
					? Enum.Parse<DatabaseConnectorType>(connectorType, true) : (DatabaseConnectorType?)null;
				var description = _connectionManager.Activate(nameOrConnectionString, parsedConnectorType);

				result.StatusIcon = MaterialIconKind.DatabaseCheck;
				result.StatusTitle = $"**{description}**";
				result.ResultContent = $"Database connection **{description}** is now active. The connection will be opened on the first use (db-schema or db-execute).";
				result.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				result.StatusIcon = MaterialIconKind.DatabaseOff;
				result.ResultContent = $"Failed to activate the database connection. Error: {ex.Message}";
				result.CompleteWithError();
			}

			return Task.CompletedTask;
		}

		private async Task GetSchemaAsync(ReactiveToolResult result, CancellationToken cancellationToken = default)
		{
			IDatabaseConnection connection;
			try
			{
				connection = await _connectionManager.GetCurrentAsync(cancellationToken);
			}
			catch (Exception ex)
			{
				result.StatusIcon = MaterialIconKind.DatabaseOff;
				result.ResultContent = $"Failed to open the database connection. Error: {ex.Message}";
				result.CompleteWithError();
				return;
			}

			result.StatusIcon = MaterialIconKind.DatabaseSearch;
			result.StatusTitle = "**schema**";

			try
			{
				result.UseMarkdown = true;
				result.ResultContent = await connection.GetSchemaAsync(cancellationToken);
				result.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				result.StatusIcon = MaterialIconKind.DatabaseOff;
				result.ResultContent = $"Failed to load the database schema. Error: {ex.Message}";
				result.CompleteWithError();
			}
		}

		private PreviewToolExecutionResult? ExecutePreview(string query)
		{
			if (!_connectionManager.IsActiveConfigured)
			{
				return new PreviewToolExecutionResult
				{
					InterruptingSuccess = false,
					InterruptingContent = "No active database connection. Call db-connect first."
				};
			}

			var keyword = ExtractKeyword(query);
			bool isRead = ReadKeywords.Contains(keyword);
			return new PreviewToolExecutionResult
			{
				StatusTitle = $"**{keyword}**",
				ExpectedBehaviour = isRead ? ToolBehaviour.DatabaseRead : ToolBehaviour.DatabaseChange
			};
		}

		private async Task ExecuteAsync(
			[Description("The SQL statement to execute")] string query,
			ReactiveToolResult result,
			CancellationToken cancellationToken = default)
		{
			IDatabaseConnection connection;
			try
			{
				connection = await _connectionManager.GetCurrentAsync(cancellationToken);
			}
			catch (Exception ex)
			{
				result.StatusIcon = MaterialIconKind.DatabaseOff;
				result.ResultContent = $"Failed to open the database connection. Error: {ex.Message}";
				result.CompleteWithError();
				return;
			}

			result.StatusIcon = MaterialIconKind.DatabaseSearch;
			result.StatusTitle = $"**{ExtractKeyword(query)}**";

			try
			{
				var queryResult = await connection.ExecuteQueryAsync(query, cancellationToken);

				if (queryResult.Columns.Length == 0)
				{
					result.StatusIcon = MaterialIconKind.DatabaseCheck;
					result.ResultContent = $"Query executed successfully. Rows affected: {queryResult.RowsAffected}";
					result.CompleteWithSuccess();
					return;
				}

				var sb = new StringBuilder();
				sb.AppendLine($"| {string.Join(" | ", queryResult.Columns)} |");
				sb.AppendLine($"|{string.Join("|", queryResult.Columns.Select(c => "---"))}|");
				foreach (var row in queryResult.Rows)
					sb.AppendLine($"| {string.Join(" | ", row.Select(EscapeCell))} |");

				if (queryResult.Truncated)
				{
					sb.AppendLine();
					sb.AppendLine($"> Result was truncated: only the first {queryResult.Rows.Length} rows are shown.");
				}

				result.UseMarkdown = true;
				result.ResultContent = sb.ToString();
				result.CompleteWithSuccess();
			}
			catch (Exception ex)
			{
				result.StatusIcon = MaterialIconKind.DatabaseOff;
				result.ResultContent = $"Query execution failed. Error: {ex.Message}";
				result.CompleteWithError();
			}
		}

		private static string ExtractKeyword(string query)
		{
			var trimmed = query.TrimStart();
			var end = trimmed.IndexOfAny([' ', '\t', '\r', '\n', '(', ';']);
			var keyword = end < 0 ? trimmed : trimmed[..end];
			return string.IsNullOrEmpty(keyword) ? "SQL" : keyword.ToUpperInvariant();
		}

		private static string EscapeCell(string value)
		{
			if (value.Length > 300)
				value = value[..300] + "…";
			return value.Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
		}
	}
}
