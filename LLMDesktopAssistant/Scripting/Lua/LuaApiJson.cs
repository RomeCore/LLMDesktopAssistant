using System;
using System.Globalization;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Unicode;
using MoonSharp.Interpreter;
using LLMDesktopAssistant.Utils;

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
		  Parses a strict JSON string into a Lua value.
		  Parameters:
		    - str: string — JSON string to parse
		  Returns: Lua value (table, string, number, boolean, nil)
		  Throws on invalid JSON.

		--- json.decode_tolerant(str)
		--- json.parse_tolerant(str) (alias)
		--- json.deserialize_tolerant(str) (alias)
		  Parses a lenient JSON string using a tolerant parser that handles:
		    - Single-quoted strings ('...')
		    - Missing quotes around keys/values
		    - Trailing commas in arrays/objects
		    - Unclosed brackets
		    - JavaScript comments (// and /* */)
		    - NaN, Infinity, +infinity, -infinity
		    - Wrong escape sequences
		    - Unquoted string values
		  Parameters:
		    - str: string — lenient JSON string to parse
		  Returns: Lua value (table, string, number, boolean, nil)
		  Returns nil on parse failure instead of throwing.

		--- json.encode(value, [indented])
		--- json.stringify(value, [indented]) (alias)
		--- json.serialize(value, [indented]) (alias)
		  Serializes a Lua value into a JSON string.
		  Parameters:
		    - value: any — Lua value to serialize (table, string, number, boolean, nil)
		    - indented: boolean — If true, the output will be formatted for readability
		  Returns: string

		NOTES:
		  - Use json.decode_tolerant() for parsing LLM output that often
		    contains malformed JSON (missing quotes, trailing commas, etc.)
		  - Lua tables with sequential integer keys (1..n) are encoded as JSON arrays.
		  - Lua tables with string/non-sequential keys are encoded as JSON objects.
		  - Mixed tables (array part + object part) are encoded as objects,
		    with numeric keys converted to strings.
		  - Userdata, functions, and threads are encoded as null.

		EXAMPLES:

		  -- Decode (strict)
		  local t = json.decode('{"name":"John","age":30}')
		  print(t.name) -- "John"
		  print(t.age)  -- 30

		  -- Decode (tolerant) — handles malformed JSON from LLMs
		  local t = json.decode_tolerant("{ name: 'John', age: 30, }")
		  print(t.name) -- "John"

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

		public override void Populate(Table globals, Table ns, LuaService luaService)
		{
			ns["decode"] = DynValue.NewCallback(new CallbackFunction(Decode));
			ns["parse"] = ns["decode"];
			ns["deserialize"] = ns["decode"];
			ns["decode_tolerant"] = DynValue.NewCallback(new CallbackFunction(DecodeTolerant));
			ns["parse_tolerant"] = ns["decode_tolerant"];
			ns["deserialize_tolerant"] = ns["decode_tolerant"];
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
				return JsonLuaConverter.JsonNodeToDynValue(ctx.GetScript(), node);
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

		private static DynValue DecodeTolerant(ScriptExecutionContext ctx, CallbackArguments args)
		{
			try
			{
				if (args.Count < 1)
					throw new ScriptRuntimeException("json.decode_tolerant(str): at least 1 argument expected.");

				var jsonStr = args[0];
				if (jsonStr.Type != DataType.String)
					throw new ScriptRuntimeException($"json.decode_tolerant(str): expected a string, got {jsonStr.Type}.");

				var node = TolerantJsonParser.Parse(jsonStr.String);
				return JsonLuaConverter.JsonNodeToDynValue(ctx.GetScript(), node);
			}
			catch (ScriptRuntimeException)
			{
				throw;
			}
			catch (Exception)
			{
				return DynValue.Nil;
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
				var node = JsonLuaConverter.DynValueToJsonNode(value);
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
	}
}