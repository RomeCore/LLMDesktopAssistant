using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using LLTSharp;
using LLTSharp.DataAccessors;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting
{
	public static class StructuredLuaConverter
	{
		/// <summary>
		/// Converts a JsonNode to a DynValue for use in Lua scripts.
		/// </summary>
		/// <param name="script">The Lua script to which the value will be assigned.</param>
		/// <param name="node">The JsonNode to convert.</param>
		/// <returns>The converted DynValue.</returns>
		public static DynValue JsonNodeToDynValue(Script script, JsonNode? node)
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

		/// <summary>
		/// Converts a DynValue to a JsonNode.
		/// </summary>
		/// <param name="value">The DynValue to convert.</param>
		/// <returns>The converted JsonNode.</returns>
		public static JsonNode? DynValueToJsonNode(DynValue value)
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

		public static TemplateDataAccessor DynValueToLLTSharp(DynValue value)
		{
			switch (value.Type)
			{
				case DataType.Nil:
				case DataType.Void:
					return TemplateNullAccessor.Instance;

				case DataType.Boolean:
					return new TemplateBooleanAccessor(value.Boolean);

				case DataType.Number:
					return new TemplateNumberAccessor(value.Number);

				case DataType.String:
					return new TemplateStringAccessor(value.String);

				case DataType.Table:
					var table = value.Table;
					if (IsArray(table))
					{
						var arr = new List<TemplateDataAccessor>();
						for (int i = 1; i <= table.Length; i++)
							arr.Add(DynValueToLLTSharp(table.Get(i)));
						return new TemplateArrayAccessor(arr);
					}
					else
					{
						var obj = new Dictionary<string, TemplateDataAccessor>();
						foreach (var kvp in table.Pairs)
						{
							string? key = KeyToString(kvp.Key);
							if (key != null)
								obj[key] = DynValueToLLTSharp(kvp.Value);
						}
						return new TemplateDictionaryAccessor(obj);
					}

				default:
					return TemplateNullAccessor.Instance;
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
