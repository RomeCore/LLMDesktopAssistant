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
			This allows overriding default behaviour for
			numbers, strings, booleans, functions, nil, and void.

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

			--- KNOWN LIMITATIONS (MoonSharp implementation) ---

			Due to MoonSharp's implementation, several metamethods do NOT work
			when set via settypemetatable, even though they are stored in the
			type metatable and visible via gettypemetatable:

			  ❌ Arithmetic operators: __add, __sub, __mul, __div, __mod, __pow, __unm
			     (standard arithmetic is always used)
			  ❌ Comparison operators: __eq, __lt, __le
			     (standard comparison is always used)
			  ❌ __concat (concatenation via ..)
			  ❌ __gc (garbage collection)
			  ❌ __len for strings (always returns character count)
			  ❌ __tostring for function type (always shows "function: ADDRESS")
			  ❌ print() ignores __tostring (use explicit tostring() instead)
			  ❌ Concatenation (..) ignores __tostring for booleans
			     (causes "attempt to concatenate a boolean value" error)

			✅ Metamethods that DO work:
			  - __tostring — but only via tostring() function, NOT via print() or ..
			  - __index — for string (native) and number (via colon syntax)
			  - __newindex — for string
			  - __call — for number
			  - __len — for number
			  - __pairs — for number

			EXAMPLES:

			  -- 1. Custom number formatting (use tostring(), not print())
			  settypemetatable("number", {
			    __tostring = function(n)
			      return string.format("%.2f", n)
			    end
			  })
			  print(tostring(3.14159)) -- "3.14"
			  -- NOTE: print(3.14159) would STILL show "3.14159"

			  -- 2. Using gettypemetatable
			  local mt = gettypemetatable("number")
			  print(mt) -- the number metatable

			  -- 3. Adding methods to numbers via __index (colon syntax!)
			  settypemetatable("number", {
			    __index = {
			      double = function(self) return self * 2 end,
			      is_even = function(self) return self % 2 == 0 end,
			      hex = function(self) return string.format("%X", self) end
			    }
			  })
			  print((5):double())    -- 10  (use colon for method call)
			  print((7):is_even())    -- false
			  print((255):hex())      -- "FF"
			  -- Dot syntax needs explicit self: (5).double(5)

			  -- 4. Boolean __tostring (via tostring)
			  settypemetatable("boolean", {
			    __tostring = function(b)
			      return b and "✓ Yes" or "✗ No"
			    end
			  })
			  print(tostring(true))  -- "✓ Yes"
			  print(tostring(false)) -- "✗ No"
			  -- NOTE: print(true) still shows "true"

			  -- 5. Custom __len for numbers
			  settypemetatable("number", {
			    __len = function(n)
			      return n % 10
			    end
			  })
			  print(#42)  -- 2  (42 % 10 = 2)

			  -- 6. Custom pairs iteration over numbers
			  settypemetatable("number", {
			    __pairs = function(n)
			      local i = 0
			      return function()
			        i = i + 1
			        if i <= n then return i, i ^ 2 end
			      end
			    end
			  })
			  for k, v in pairs(5) do print(k, v) end
			  -- 1  1
			  -- 2  4
			  -- 3  9
			  -- 4  16
			  -- 5  25

			  -- 7. Using __call on numbers
			  settypemetatable("number", {
			    __call = function(n, multiplier)
			      return n * multiplier
			    end
			  })
			  print((5)(3))  -- 15

			  -- 8. Extend: add __tostring without removing existing keys
			  extendtypemetatable("number", {
			    __tostring = function(n)
			      return "«" .. n .. "»"
			    end
			  })

			  -- 9. Clear/reset a type metatable
			  settypemetatable("number", nil)
			  -- After reset: tostring(42) returns "42" (default)

			  -- 10. nil __tostring
			  settypemetatable("nil", {
			    __tostring = function() return "N/A" end
			  })
			  print(tostring(nil)) -- "N/A"

			  -- 11. __newindex for strings (intercepts field assignment)
			  settypemetatable("string", {
			    __newindex = function(t, key, value)
			      print("Cannot set " .. key .. " on string")
			    end
			  })
			  local s = "hello"
			  s.custom_field = 42  -- triggers __newindex

			  -- 12. Default string metatable must be restored after clearing
			  settypemetatable("string", nil)
			  -- s:upper() would now fail!
			  settypemetatable("string", { __index = string })
			  -- Now s:upper() works again

			NOTES:
			  - Changes affect ALL values of the type globally in the Lua runtime.
			  - Use with care — inappropriate metamethods can break Lua semantics.
			  - Pass nil to clear/reset a type metatable to default.
			  - extendtypemetatable is additive: it only sets or overwrites specific keys.
			  - Only 6 types are supported: "nil", "void", "boolean", "number", "string", "function".
			    Types like "table", "userdata", "thread" cannot be used.
			  - String type has a default metatable with __index = string table.
			    Clearing it removes all string methods until __index is restored.
			  - print() NEVER uses __tostring — it outputs raw values.
			    Always use tostring() when you need custom formatting.
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

			var metatable = script.GetTypeMetatable(dataType.Value)?.DeepMergeWith(extensions) ?? extensions;
			script.SetTypeMetatable(dataType.Value, metatable);

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
