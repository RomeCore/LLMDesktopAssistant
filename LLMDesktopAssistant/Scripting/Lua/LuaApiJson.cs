using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Serialization.Json;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API for JSON encoding/decoding: <c>json.*</c>.
	/// Registered in the global namespace as "json".
	/// Uses MoonSharp's built-in JSON serializer under the hood.
	/// </summary>
	[LuaApi]
	public class LuaApiJson : LuaApiBase
	{
		public override string? Namespace => "json";

		public override string? Manuals => """
			--- json — JSON encoding and decoding API

			Provides JSON serialization and deserialization functions.
			Handles null, tables, arrays, strings, numbers and booleans.

			FUNCTIONS:

			--- json.decode(str)
			  Parses a JSON string into a Lua value.
			  Parameters:
			    - str: string — JSON string to parse
			  Returns: Lua value (table, string, number, boolean, nil)

			--- json.encode(value)
			  Serializes a Lua value into a JSON string.
			  Parameters:
			    - value: any — Lua value to serialize (table, string, number, boolean, nil)
			  Returns: string

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

		public override void Populate(Table globals, Table ns)
		{
			ns["decode"] = DynValue.NewCallback(new CallbackFunction(Decode));
			ns["encode"] = DynValue.NewCallback(new CallbackFunction(Encode));
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
					throw new ScriptRuntimeException("json.encode(value): at least 1 argument expected.");

				var value = args[0];
				string s = JsonTableConverter.ObjectToJson(value);
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
	}
}
