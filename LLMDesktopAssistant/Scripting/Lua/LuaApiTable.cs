using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API with extended table utilities: <c>table.*</c>.
	/// Supplements the built-in table library with commonly needed functions.
	/// </summary>
	[LuaApi]
	public class LuaApiTable : LuaApiBase
	{
		public override string? Namespace => "table";

		public override string? Manuals => """
			--- table — extended table utilities

			Supplements the built-in Lua table library.

			FUNCTIONS:

			--- table.contains(t, value)
			  Checks if a value exists in the table (shallow comparison).
			  Parameters:
			    - t: table — table to search
			    - value: any — value to find
			  Returns: boolean

			--- table.find(t, value)
			  Returns the first key/index where the value is found, or nil.
			  Parameters:
			    - t: table — table to search
			    - value: any — value to find
			  Returns: key or nil

			--- table.map(t, func)
			  Returns a new table where each element is transformed by func(value, key).
			  Parameters:
			    - t: table — source table
			    - func: function(value, key) -> new_value
			  Returns: table

			--- table.filter(t, func)
			  Returns a new table with elements where func(value, key) returns true.
			  Parameters:
			    - t: table — source table
			    - func: function(value, key) -> boolean
			  Returns: table

			--- table.reduce(t, func, initial)
			  Reduces the table to a single value using a binary function.
			  Parameters:
			    - t: table — source table
			    - func: function(accumulator, value, key) -> new_accumulator
			    - initial: any — initial accumulator value
			  Returns: any — final accumulator

			--- table.slice(t, start, [end])
			  Returns a slice of the array (1-based inclusive).
			  Parameters:
			    - t: table — array table
			    - start: number — start index (1-based)
			    - end: number (optional) — end index (inclusive), default: #t
			  Returns: table — new array

			--- table.keys(t)
			  Returns an array of all keys in the table.
			  Parameters:
			    - t: table — source table
			  Returns: table — array of keys

			--- table.values(t)
			  Returns an array of all values in the table.
			  Parameters:
			    - t: table — source table
			  Returns: table — array of values

			--- table.merge(t1, t2)
			  Merges t2 into t1 (modifies t1). For arrays, appends elements.
			  For objects, overwrites/replaces keys from t2.
			  Parameters:
			    - t1: table — target table (mutated)
			    - t2: table — source table
			  Returns: table — t1 (mutated)

			--- table.clone(t)
			  Returns a shallow copy of the table.
			  Parameters:
			    - t: table — source table
			  Returns: table — copy
			
			--- table.deep_clone(t)
			  Returns a deep copy of the table.
			  Parameters:
			    - t: table — source table
			  Returns: table — copy
			
			--- table.is_empty(t)
			  Returns true if the table has no elements (array part and hash part).
			  Parameters:
			    - t: table — source table
			  Returns: boolean

			--- table.size(t)
			  Returns the total number of elements in the table (all keys).
			  Parameters:
			    - t: table — source table
			  Returns: number

			--- table.first(t)
			--- table.last(t)
			  Returns the first/last element of an array table, or nil if empty.
			  Parameters:
			    - t: table — array table
			  Returns: element or nil

			--- table.each(t, func)
			  Iterates over all elements, calling func(value, key) for each.
			  Parameters:
			    - t: table — source table
			    - func: function(value, key)
			  Returns: nil

			EXAMPLES:

			  local t = {1, 2, 3, 4, 5}
			  print(table.contains(t, 3)) -- true
			  print(table.find(t, 4))     -- 4
			  local doubled = table.map(t, function(v) return v * 2 end)
			  local evens = table.filter(t, function(v) return v % 2 == 0 end)
			  local sum = table.reduce(t, function(a, v) return a + v end, 0)
			  local head = table.slice(t, 1, 3) -- {1, 2, 3}
			  print(table.first(t)) -- 1
			  print(table.last(t))  -- 5
			""";

		public override void Populate(Table globals, Table ns)
		{
			ns["contains"] = DynValue.NewCallback(new CallbackFunction(Contains));
			ns["find"] = DynValue.NewCallback(new CallbackFunction(Find));
			ns["map"] = DynValue.NewCallback(new CallbackFunction(Map));
			ns["filter"] = DynValue.NewCallback(new CallbackFunction(Filter));
			ns["reduce"] = DynValue.NewCallback(new CallbackFunction(Reduce));
			ns["slice"] = DynValue.NewCallback(new CallbackFunction(Slice));
			ns["keys"] = DynValue.NewCallback(new CallbackFunction(Keys));
			ns["values"] = DynValue.NewCallback(new CallbackFunction(Values));
			ns["merge"] = DynValue.NewCallback(new CallbackFunction(Merge));
			ns["clone"] = DynValue.NewCallback(new CallbackFunction(Clone));
			ns["deep_clone"] = DynValue.NewCallback(new CallbackFunction(DeepClone));
			ns["is_empty"] = DynValue.NewCallback(new CallbackFunction(IsEmpty));
			ns["size"] = DynValue.NewCallback(new CallbackFunction(Size));
			ns["first"] = DynValue.NewCallback(new CallbackFunction(First));
			ns["last"] = DynValue.NewCallback(new CallbackFunction(Last));
			ns["each"] = DynValue.NewCallback(new CallbackFunction(Each));
		}

		private static DynValue Contains(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("table.contains(t, value): at least 2 arguments expected.");
			var t = args[0];
			if (t.Type != DataType.Table)
				throw new ScriptRuntimeException("table.contains(): first argument must be a table.");
			var value = args[1];

			foreach (var kv in t.Table.Pairs)
			{
				if (ValuesEqual(kv.Value, value))
					return DynValue.True;
			}
			return DynValue.False;
		}

		private static DynValue Find(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("table.find(t, value): at least 2 arguments expected.");
			var t = args[0];
			if (t.Type != DataType.Table)
				throw new ScriptRuntimeException("table.find(): first argument must be a table.");
			var value = args[1];

			foreach (var kv in t.Table.Pairs)
			{
				if (ValuesEqual(kv.Value, value))
					return kv.Key;
			}
			return DynValue.Nil;
		}

		private static DynValue Map(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("table.map(t, func): at least 2 arguments expected.");
			var t = args[0];
			if (t.Type != DataType.Table)
				throw new ScriptRuntimeException("table.map(): first argument must be a table.");
			var func = args[1];
			if (func.Type != DataType.Function)
				throw new ScriptRuntimeException("table.map(): second argument must be a function.");

			var result = new Table(ctx.OwnerScript);
			foreach (var kv in t.Table.Pairs)
			{
				var newVal = ctx.GetScript().Call(func, kv.Value, kv.Key);
				result.Set(kv.Key, newVal);
			}
			return DynValue.NewTable(result);
		}

		private static DynValue Filter(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("table.filter(t, func): at least 2 arguments expected.");
			var t = args[0];
			if (t.Type != DataType.Table)
				throw new ScriptRuntimeException("table.filter(): first argument must be a table.");
			var func = args[1];
			if (func.Type != DataType.Function)
				throw new ScriptRuntimeException("table.filter(): second argument must be a function.");

			var script = ctx.OwnerScript;
			var isArray = IsArray(t.Table);

			if (isArray)
			{
				var result = new Table(script);
				int idx = 1;
				for (int i = 1; i <= t.Table.Length; i++)
				{
					var val = t.Table.Get(i);
					var keep = script.Call(func, val, DynValue.NewNumber(i));
					if (keep.CastToBool())
					{
						result[idx] = val;
						idx++;
					}
				}
				return DynValue.NewTable(result);
			}
			else
			{
				var result = new Table(script);
				foreach (var kv in t.Table.Pairs)
				{
					var keep = script.Call(func, kv.Value, kv.Key);
					if (keep.CastToBool())
						result.Set(kv.Key, kv.Value);
				}
				return DynValue.NewTable(result);
			}
		}

		private static bool IsArray(Table t)
		{
			int len = t.Length;
			if (len == 0)
			{
				// Empty table with no hash keys is an array
				foreach (var _ in t.Pairs)
					return false;
				return true;
			}
			// Check that all keys 1..len exist and no non-numeric keys
			for (int i = 1; i <= len; i++)
			{
				if (t.Get(i).Type == DataType.Nil)
					return false;
			}
			// Check there are no non-numeric keys
			foreach (var kv in t.Pairs)
			{
				if (kv.Key.Type != DataType.Number)
					return false;
			}
			return true;
		}

		private static DynValue Reduce(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 3)
				throw new ScriptRuntimeException("table.reduce(t, func, initial): at least 3 arguments expected.");
			var t = args[0];
			if (t.Type != DataType.Table)
				throw new ScriptRuntimeException("table.reduce(): first argument must be a table.");
			var func = args[1];
			if (func.Type != DataType.Function)
				throw new ScriptRuntimeException("table.reduce(): second argument must be a function.");
			var acc = args[2];

			foreach (var kv in t.Table.Pairs)
			{
				acc = ctx.GetScript().Call(func, acc, kv.Value, kv.Key);
			}
			return acc;
		}

		private static DynValue Slice(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("table.slice(t, start, [end]): at least 2 arguments expected.");
			var t = args[0];
			if (t.Type != DataType.Table)
				throw new ScriptRuntimeException("table.slice(): first argument must be a table.");
			var startVal = args[1].CastToNumber();
			if (startVal == null)
				throw new ScriptRuntimeException("table.slice(): start must be a number.");
			int start = (int)startVal.Value;

			int len = t.Table.Length;
			int end = len;
			if (args.Count > 2 && !args[2].IsNil())
			{
				var endVal = args[2].CastToNumber();
				if (endVal == null)
					throw new ScriptRuntimeException("table.slice(): end must be a number.");
				end = (int)endVal.Value;
			}

			// Clamp
			if (start < 1) start = 1;
			if (end > len) end = len;

			var result = new Table(ctx.OwnerScript);
			if (start > end)
				return DynValue.NewTable(result);

			int idx = 1;
			for (int i = start; i <= end; i++)
			{
				result[idx] = t.Table.Get(i);
				idx++;
			}
			return DynValue.NewTable(result);
		}

		private static DynValue Keys(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("table.keys(t): at least 1 argument expected.");
			var t = args[0];
			if (t.Type != DataType.Table)
				throw new ScriptRuntimeException("table.keys(): first argument must be a table.");

			var result = new Table(ctx.OwnerScript);
			int idx = 1;
			foreach (var kv in t.Table.Pairs)
			{
				result[idx] = kv.Key;
				idx++;
			}
			return DynValue.NewTable(result);
		}

		private static DynValue Values(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("table.values(t): at least 1 argument expected.");
			var t = args[0];
			if (t.Type != DataType.Table)
				throw new ScriptRuntimeException("table.values(): first argument must be a table.");

			var result = new Table(ctx.OwnerScript);
			int idx = 1;
			foreach (var kv in t.Table.Pairs)
			{
				result[idx] = kv.Value;
				idx++;
			}
			return DynValue.NewTable(result);
		}

		private static DynValue Merge(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("table.merge(t1, t2): at least 2 arguments expected.");
			var t1 = args[0];
			var t2 = args[1];
			if (t1.Type != DataType.Table || t2.Type != DataType.Table)
				throw new ScriptRuntimeException("table.merge(): both arguments must be tables.");

			var target = t1.Table;
			var source = t2.Table;

			int targetLen = target.Length;

			// If both are arrays, append
			if (targetLen > 0)
			{
				foreach (var kv in source.Pairs)
				{
					if (kv.Key.Type == DataType.Number)
					{
						var idx = targetLen + (int)kv.Key.Number;
						target.Set(idx, kv.Value);
					}
					else
					{
						target.Set(kv.Key, kv.Value);
					}
				}
			}
			else
			{
				// Object merge
				foreach (var kv in source.Pairs)
					target.Set(kv.Key, kv.Value);
			}

			return t1;
		}

		private static DynValue Clone(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("table.clone(t): at least 1 argument expected.");
			var t = args[0];
			if (t.Type != DataType.Table)
				throw new ScriptRuntimeException("table.clone(): first argument must be a table.");

			return DynValue.NewTable(t.Table.ShallowClone());
		}

		private static DynValue DeepClone(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("table.deep_clone(t): at least 1 argument expected.");
			var t = args[0];
			if (t.Type != DataType.Table)
				throw new ScriptRuntimeException("table.deep_clone(): first argument must be a table.");

			return DynValue.NewTable(t.Table.DeepClone());
		}

		private static DynValue IsEmpty(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("table.is_empty(t): at least 1 argument expected.");
			var t = args[0];
			if (t.Type != DataType.Table)
				throw new ScriptRuntimeException("table.is_empty(): first argument must be a table.");

			// Check if there's any key
			foreach (var _ in t.Table.Pairs)
				return DynValue.False;
			return DynValue.True;
		}

		private static DynValue Size(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("table.size(t): at least 1 argument expected.");
			var t = args[0];
			if (t.Type != DataType.Table)
				throw new ScriptRuntimeException("table.size(): first argument must be a table.");

			int count = 0;
			foreach (var _ in t.Table.Pairs)
				count++;
			return DynValue.NewNumber(count);
		}

		private static DynValue First(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("table.first(t): at least 1 argument expected.");
			var t = args[0];
			if (t.Type != DataType.Table)
				throw new ScriptRuntimeException("table.first(): first argument must be a table.");
			if (t.Table.Length == 0)
				return DynValue.Nil;
			return t.Table.Get(1);
		}

		private static DynValue Last(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("table.last(t): at least 1 argument expected.");
			var t = args[0];
			if (t.Type != DataType.Table)
				throw new ScriptRuntimeException("table.last(): first argument must be a table.");
			int len = t.Table.Length;
			if (len == 0)
				return DynValue.Nil;
			return t.Table.Get(len);
		}

		private static DynValue Each(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("table.each(t, func): at least 2 arguments expected.");
			var t = args[0];
			if (t.Type != DataType.Table)
				throw new ScriptRuntimeException("table.each(): first argument must be a table.");
			var func = args[1];
			if (func.Type != DataType.Function)
				throw new ScriptRuntimeException("table.each(): second argument must be a function.");

			foreach (var kv in t.Table.Pairs)
				ctx.GetScript().Call(func, kv.Value, kv.Key);
			return DynValue.Nil;
		}

		// --- Helper ---

		private static bool ValuesEqual(DynValue a, DynValue b)
		{
			if (a.Type != b.Type)
				return false;
			switch (a.Type)
			{
				case DataType.Nil: return true;
				case DataType.Boolean: return a.Boolean == b.Boolean;
				case DataType.Number: return a.Number == b.Number;
				case DataType.String: return a.String == b.String;
				default: return a.Equals(b);
			}
		}
	}
}
