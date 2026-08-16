using LLMDesktopAssistant.Data.Connectors;

namespace LLMDesktopAssistant.Tests.Database
{
	public class DatabaseConnectorTests
	{
		private static async Task<IDatabaseConnection> CreateConnectionAsync()
		{
			var connector = new SqliteDatabaseConnector();
			return await connector.ConnectAsync("Data Source=:memory:");
		}

		[Fact]
		public async Task ExecuteQuery_Select_ReturnsColumnsAndRows()
		{
			await using var connection = await CreateConnectionAsync();
			await connection.ExecuteQueryAsync("CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT NOT NULL)");
			await connection.ExecuteQueryAsync("INSERT INTO users (name) VALUES ('Alice'), ('Bob')");

			var result = await connection.ExecuteQueryAsync("SELECT id, name FROM users ORDER BY id");

			Assert.Equal(new[] { "id", "name" }, result.Columns);
			Assert.Equal(2, result.Rows.Length);
			Assert.Equal(new[] { "1", "Alice" }, result.Rows[0]);
			Assert.Equal(new[] { "2", "Bob" }, result.Rows[1]);
			Assert.False(result.Truncated);
		}

		[Fact]
		public async Task ExecuteQuery_Modification_ReturnsRowsAffected()
		{
			await using var connection = await CreateConnectionAsync();
			await connection.ExecuteQueryAsync("CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT NOT NULL)");

			var insert = await connection.ExecuteQueryAsync("INSERT INTO users (name) VALUES ('Alice'), ('Bob'), ('Carol')");
			Assert.Empty(insert.Columns);
			Assert.Equal(3, insert.RowsAffected);

			var update = await connection.ExecuteQueryAsync("UPDATE users SET name = 'A' WHERE id = 1");
			Assert.Equal(1, update.RowsAffected);
		}

		[Fact]
		public async Task ExecuteQuery_Select_NullValuesReturnedAsNull()
		{
			await using var connection = await CreateConnectionAsync();
			await connection.ExecuteQueryAsync("CREATE TABLE items (id INTEGER PRIMARY KEY, note TEXT)");
			await connection.ExecuteQueryAsync("INSERT INTO items (note) VALUES (NULL)");

			var result = await connection.ExecuteQueryAsync("SELECT id, note FROM items");

			Assert.Equal(new[] { "1", "NULL" }, result.Rows[0]);
		}

		[Fact]
		public async Task ExecuteQuery_LargeResultSet_IsTruncated()
		{
			await using var connection = await CreateConnectionAsync();
			await connection.ExecuteQueryAsync("CREATE TABLE numbers (n INTEGER)");
			for (int batch = 0; batch < 10; batch++)
			{
				var values = string.Join(", ", Enumerable.Range(batch * 100 + 1, 100).Select(i => $"({i})"));
				await connection.ExecuteQueryAsync($"INSERT INTO numbers VALUES {values}");
			}

			var result = await connection.ExecuteQueryAsync("SELECT n FROM numbers");

			Assert.True(result.Truncated);
			Assert.Equal(500, result.Rows.Length);
		}

		[Fact]
		public async Task GetSchema_ReturnsTablesAndColumns()
		{
			await using var connection = await CreateConnectionAsync();
			await connection.ExecuteQueryAsync("CREATE TABLE users (id INTEGER PRIMARY KEY, name TEXT NOT NULL)");
			await connection.ExecuteQueryAsync("CREATE VIEW user_names AS SELECT name FROM users");

			var schema = await connection.GetSchemaAsync();

			Assert.Contains("## users (table)", schema);
			Assert.Contains("| id | INTEGER |", schema);
			Assert.Contains("## user_names (view)", schema);
		}

		[Fact]
		public async Task GetSchema_EmptyDatabase_ReturnsNoTablesMessage()
		{
			await using var connection = await CreateConnectionAsync();

			var schema = await connection.GetSchemaAsync();

			Assert.Contains("No tables or views found.", schema);
		}
	}
}
