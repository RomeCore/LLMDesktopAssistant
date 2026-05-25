using System;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API for JSON encoding/decoding: <c>json.*</c>.
	/// Registered in the global namespace as "json".
	/// Uses System.Text.Json.Nodes for both encoding and decoding.
	/// </summary>
	[LuaApi]
	public class LuaApiJson : LuaApiBase
	{
		public override string? Namespace => "json";

		public override string? Manuals => """
		--- json — JSON encoding and decoding API

		Provides JSON serialization and deserialization functions.
		Handles nil, tables, arrays, strings, numbers and booleans.

		FUNCTIONS:

		--- json.decode(str)
		--- json.parse(str) (alias)
		--- json.deserialize(str) (alias)
		  Parses a JSON string into a Lua value.
		  Parameters:
		    - str: string — JSON string to parse
		  Returns: Lua value (table, string, number, boolean, nil)

		--- json.encode(value, [indented])
		--- json.stringify(value, [indented]) (alias)
		--- json.serialize(value, [indented]) (alias)
		  Serializes a Lua value into a JSON string.
		  Parameters:
		    - value: any — Lua value to serialize (table, string, number, boolean, nil)
		    - indented: boolean — If true, the output will be formatted for readability
		  Returns: string

		NOTES:
		  - Lua tables with sequential integer keys (1..n) are encoded as JSON arrays.
		  - Lua tables with string/non-sequential keys are encoded as JSON objects.
		  - Mixed tables (array part + object part) are encoded as objects,
		    with numeric keys converted to strings.
		  - Userdata, functions, and threads are encoded as null.

		EXAMPLES:

		  -- Decode
		  local t = json.decode('{"name":"John","age":30}')
		  print(t.name) -- "John"
		  print(t.age)  -- 30

		  -- Encode
		  local s = json.encode({hello = "world", num = 42})
		  print(s) -- '{"hello":"world","num":42}'

		  -- Encode array
		  local s = json.encode({"a", "b", "c"})
		  print(s) -- '["a","b","c"]'
		""";

		private static readonly JsonSerializerOptions _serializationOptions = new()
		{
			Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
		};

		private static readonly JsonSerializerOptions _indentedSerializationOptions = new()
		{
			Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
			WriteIndented = true
		};

		public override void Populate(Table globals, Table ns)
		{
			ns["decode"] = DynValue.NewCallback(new CallbackFunction(Decode));
			ns["parse"] = ns["decode"];
			ns["deserialize"] = ns["decode"];
			ns["encode"] = DynValue.NewCallback(new CallbackFunction(Encode));
			ns["stringify"] = ns["encode"];
			ns["serialize"] = ns["encode"];
		}

		private static DynValue Decode(ScriptExecutionContext ctx, CallbackArguments args)
		{
			try
			{
				if (args.Count < 1)
					throw new ScriptRuntimeException("json.decode(str): at least 1 argument expected.");

				var jsonStr = args[0];
				if (jsonStr.Type != DataType.String)
					throw new ScriptRuntimeException($"json.decode(str): expected a string, got {jsonStr.Type}.");

				var node = JsonNode.Parse(jsonStr.String);
				return JsonNodeToDynValue(ctx.GetScript(), node);
			}
			catch (ScriptRuntimeException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"JSON decode error: {ex.Message}");
			}
		}

		private static DynValue Encode(ScriptExecutionContext ctx, CallbackArguments args)
		{
			try
			{
				if (args.Count < 1)
					throw new ScriptRuntimeException("json.encode(value, [indented]): at least 1 argument expected.");

				bool indented = false;
				if (args.Count > 1 && args[1].Type == DataType.Boolean && args[1].Boolean)
					indented = true;

				var value = args[0];
				var node = DynValueToJsonNode(value);
				var options = indented ? _indentedSerializationOptions : _serializationOptions;
				string s = node?.ToJsonString(options) ?? "null";
				return DynValue.NewString(s);
			}
			catch (ScriptRuntimeException)
			{
				throw;
			}
			catch (Exception ex)
			{
				throw new ScriptRuntimeException($"JSON encode error: {ex.Message}");
			}
		}

		private static DynValue JsonNodeToDynValue(Script script, JsonNode? node)
		{
			if (node == null)
				return DynValue.Nil;

			switch (node)
			{
				case JsonObject obj:
					var table = new Table(script);
					foreach (var prop in obj)
						table[prop.Key] = JsonNodeToDynValue(script, prop.Value);
					return DynValue.NewTable(table);

				case JsonArray arr:
					var arrTable = new Table(script);
					for (int i = 0; i < arr.Count; i++)
						arrTable[i + 1] = JsonNodeToDynValue(script, arr[i]);
					return DynValue.NewTable(arrTable);

				case JsonValue val:
					if (val.TryGetValue(out string? s))
						return DynValue.NewString(s);
					if (val.TryGetValue(out long l))
						return DynValue.NewNumber(l);
					if (val.TryGetValue(out double d))
						return DynValue.NewNumber(d);
					if (val.TryGetValue(out bool b))
						return DynValue.NewBoolean(b);
					return DynValue.Nil;

				default:
					return DynValue.Nil;
			}
		}

		private static JsonNode? DynValueToJsonNode(DynValue value)
		{
			switch (value.Type)
			{
				case DataType.Nil:
				case DataType.Void:
					return null;

				case DataType.Boolean:
					return JsonValue.Create(value.Boolean);

				case DataType.Number:
					return JsonValue.Create(value.Number);

				case DataType.String:
					return JsonValue.Create(value.String);

				case DataType.Table:
					var table = value.Table;
					if (IsArray(table))
					{
						var arr = new JsonArray();
						for (int i = 1; i <= table.Length; i++)
							arr.Add(DynValueToJsonNode(table.Get(i)));
						return arr;
					}
					else
					{
						var obj = new JsonObject();
						foreach (var kvp in table.Pairs)
						{
							string? key = KeyToString(kvp.Key);
							if (key != null)
								obj[key] = DynValueToJsonNode(kvp.Value);
						}
						return obj;
					}

				default:
					return null;
			}
		}

		/// <summary>
		/// Determines whether a Lua table should be serialized as a JSON array.
		/// A table is considered an array if it has a contiguous sequence of
		/// integer keys from 1 to <see cref="Table.Length"/> and no keys
		/// outside that range.
		/// </summary>
		private static bool IsArray(Table table)
		{
			int len = table.Length;

			if (len == 0 && table.Keys.Count() == 0)
				return true;

			if (len == 0)
				return false;

			for (int i = 1; i <= len; i++)
			{
				if (!table.Keys.Contains(DynValue.NewNumber(i)))
					return false;
			}

			foreach (var key in table.Keys)
			{
				if (key.Type != DataType.Number)
					return false;
				double num = key.Number;
				if (num < 1 || num > len || num != Math.Truncate(num))
					return false;
			}

			return true;
		}

		/// <summary>
		/// Converts a Lua key to a JSON object key string.
		/// Returns null for keys that cannot be represented (e.g. table keys).
		/// </summary>
		private static string? KeyToString(DynValue key)
		{
			switch (key.Type)
			{
				case DataType.String:
					return key.String;
				case DataType.Number:
					return key.Number.ToString(CultureInfo.InvariantCulture);
				default:
					return null;
			}
		}
	}
}