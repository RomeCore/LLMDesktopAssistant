using System;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API for type metatable manipulation.
	/// Provides global functions <c>gettypemetatable</c>, <c>settypemetatable</c>,
	/// and <c>extendtypemetatable</c> for working with MoonSharp's type metatables
	/// on primitive Lua types.
	/// 
	/// Type metatables let you override default behavior of ALL values of a given type,
	/// similar to how regular metatables work on tables.
	/// 
	/// Supported type names: "nil", "boolean", "number", "string", "function", "void"
	/// </summary>
	[LuaApi(chatScoped: false)]
	public class LuaApiTypeSystem : LuaApiBase
	{
		public override string? Namespace => null;

		public override string? Manuals => """
			--- gettypemetatable(type) — get type metatable for a primitive Lua type
			
			Returns the current metatable for ALL values of a primitive Lua type.
			
			Parameters:
			  - type: string — type name (same as settypemetatable).
			
			Returns: table or nil — the current type metatable.
			
			--- settypemetatable(type, metatable) — set type metatable for a primitive Lua type

			Sets the metatable for ALL values of a primitive Lua type.
			This allows overriding default behaviour (__tostring, __add, __eq, etc.)
			for numbers, strings, booleans, functions, nil, and void.

			Parameters:
			  - type: string — type name:
			    "nil", "void", "boolean", "number", "string", "function"
			    (also accepts short aliases: "bool", "num", "str", "func")
			  - metatable: table or nil — the metatable to set, or nil to clear.

			Returns: nil

			Throws if the type is not recognized.

			--- extendtypemetatable(type, extensions) — merge fields into the current type metatable

			Merges the provided table into the existing type metatable for the given type,
			rather than replacing it entirely. Useful for adding individual metamethods
			without overwriting ones already set.

			Parameters:
			  - type: string — type name (same as settypemetatable).
			  - extensions: table — table whose key-value pairs will be merged into
			    the type metatable. Overwrites existing keys if they match.

			Returns: nil

			Throws if the type is not recognized.

			EXAMPLES:

			  -- 1. Custom number formatting
			  settypemetatable("number", {
			    __tostring = function(n)
			      return string.format("%.2f", n)
			    end
			  })
			  print(3.14159) -- "3.14"

			  -- 2. Using string name
			  local mt = gettypemetatable("number")
			  print(mt) -- the number metatable

			  -- 3. Safe number addition (with overflow check)
			  settypemetatable("number", {
			    __add = function(a, b)
			      local result = a + b
			      if result > 1e9 then
			        error("Number too large!")
			      end
			      return result
			    end
			  })

			  -- 4. Boolean pretty printing
			  settypemetatable("boolean", {
			    __tostring = function(b)
			      return b and "✓ Yes" or "✗ No"
			    end
			  })
			  print(true)  -- "✓ Yes"
			  print(false) -- "✗ No"

			  -- 5. String with length in words (not characters)
			  settypemetatable("string", {
			    __len = function(s)
			      local count = 0
			      for _ in s:gmatch("%S+") do count = count + 1 end
			      return count
			    end
			  })
			  print(#"Hello world foo") -- 3

			  -- 6. Case-insensitive string equality
			  settypemetatable("string", {
			    __eq = function(a, b)
			      return a:lower() == b:lower()
			    end
			  })
			  print("Hello" == "hello") --> true

			  -- 7. Extend: add __tostring without removing __eq
			  extendtypemetatable("string", {
			    __tostring = function(s)
			      return "«" .. s .. "»"
			    end
			  })

			  -- 8. Clear/reset a type metatable
			  settypemetatable("number", nil)

			NOTES:
			  - Changes affect ALL values of the type globally in the Lua runtime.
			  - Use with care — inappropriate metamethods can break Lua semantics.
			  - Pass nil to clear/reset a type metatable to default.
			  - extendtypemetatable is additive: it only sets or overwrites specific keys.
			""";

		public override void Populate(Table globals, Table ns, LuaService luaService)
		{
			globals["gettypemetatable"] = DynValue.NewCallback(new CallbackFunction(GetTypeMetatable));
			globals["settypemetatable"] = DynValue.NewCallback(new CallbackFunction(SetTypeMetatable));
			globals["extendtypemetatable"] = DynValue.NewCallback(new CallbackFunction(ExtendTypeMetatable));
		}

		private static DynValue GetTypeMetatable(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1 || args[0].Type != DataType.String)
				return DynValue.Nil;

			var dataType = ParseDataType(args[0].String);
			if (dataType == null)
				return DynValue.Nil;

			var mt = ctx.OwnerScript.GetTypeMetatable(dataType.Value);
			return mt != null ? DynValue.NewTable(mt) : DynValue.Nil;
		}

		private static DynValue SetTypeMetatable(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1 || args[0].Type != DataType.String)
				throw new ScriptRuntimeException("settypemetatable(type, [metatable]): first argument must be a type name string.");

			var dataType = ParseDataType(args[0].String);
			if (dataType == null)
				throw new ScriptRuntimeException($"settypemetatable: unknown type '{args[0].String}'. " +
					"Supported: \"nil\", \"void\", \"boolean\", \"number\", \"string\", \"function\".");

			Table? metatable = null;
			if (args.Count >= 2)
			{
				if (args[1].Type == DataType.Table)
					metatable = args[1].Table;
				else if (args[1].Type != DataType.Nil)
					throw new ScriptRuntimeException("settypemetatable: metatable must be a table or nil.");
			}

			ctx.OwnerScript.SetTypeMetatable(dataType.Value, metatable);
			return DynValue.Nil;
		}

		private static DynValue ExtendTypeMetatable(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1 || args[0].Type != DataType.String)
				throw new ScriptRuntimeException("extendtypemetatable(type, extensions): first argument must be a type name string.");

			if (args.Count < 2 || args[1].Type != DataType.Table)
				throw new ScriptRuntimeException("extendtypemetatable(type, extensions): second argument must be a table.");

			var dataType = ParseDataType(args[0].String);
			if (dataType == null)
				throw new ScriptRuntimeException($"extendtypemetatable: unknown type '{args[0].String}'. " +
					"Supported: \"nil\", \"void\", \"boolean\", \"number\", \"string\", \"function\".");

			var script = ctx.OwnerScript;
			var extensions = args[1].Table;

			// Get existing metatable or create a new one
			var existingMt = script.GetTypeMetatable(dataType.Value);
			if (existingMt == null)
			{
				existingMt = new Table(script);
				script.SetTypeMetatable(dataType.Value, existingMt);
			}

			// Merge each key from extensions into the existing metatable
			foreach (var kvp in extensions.Pairs)
			{
				existingMt.Set(kvp.Key, kvp.Value);
			}

			return DynValue.Nil;
		}

		/// <summary>
		/// Parses a Lua type name string to a MoonSharp DataType enum value.
		/// </summary>
		private static DataType? ParseDataType(string typeName)
		{
			return typeName.ToLowerInvariant() switch
			{
				"nil" => DataType.Nil,
				"void" => DataType.Void,
				"boolean" or "bool" => DataType.Boolean,
				"number" or "num" => DataType.Number,
				"string" or "str" => DataType.String,
				"function" or "func" => DataType.Function,
				_ => null
			};
		}
	}
}
