using System.Globalization;
using System.Text.Json.Nodes;
using AsyncLua.Values;
using LLTSharp;
using LLTSharp.DataAccessors;

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
		public static LuaValue JsonNodeToLuaValue(JsonNode? node)
		{
			if (node == null)
				return LuaNil.Instance;

			switch (node)
			{
				case JsonObject obj:
					var table = new LuaTable();
					foreach (var prop in obj)
						table[prop.Key] = JsonNodeToLuaValue(prop.Value);
					return table;

				case JsonArray arr:
					var arrTable = new LuaTable();
					for (int i = 0; i < arr.Count; i++)
						arrTable[i + 1] = JsonNodeToLuaValue(arr[i]);
					return arrTable;

				case JsonValue val:
					if (val.TryGetValue(out string? s))
						return new LuaString(s);
					if (val.TryGetValue(out long l))
						return new LuaNumber(l);
					if (val.TryGetValue(out double d))
						return new LuaNumber(d);
					if (val.TryGetValue(out bool b))
						return LuaBoolean.FromBoolean(b);
					return LuaNil.Instance;

				default:
					return LuaNil.Instance;
			}
		}

		/// <summary>
		/// Converts a DynValue to a JsonNode.
		/// </summary>
		/// <param name="value">The DynValue to convert.</param>
		/// <returns>The converted JsonNode.</returns>
		public static JsonNode? LuaValueToJsonNode(LuaValue value)
		{
			switch (value)
			{
				case LuaNil:
					return null;

				case LuaBoolean boolean:
					return JsonValue.Create(boolean.Value);

				case LuaNumber number:
					return JsonValue.Create(number.Value);

				case LuaString str:
					return JsonValue.Create(str.Value);

				case LuaTable table:
					if (IsArray(table))
					{
						var arr = new JsonArray();
						for (int i = 1; i <= table.Length; i++)
							arr.Add(LuaValueToJsonNode(table.Get(i)));
						return arr;
					}
					else
					{
						var obj = new JsonObject();
						foreach (var kvp in table.Entries)
						{
							string? key = KeyToString(kvp.Key);
							if (key != null)
								obj[key] = LuaValueToJsonNode(kvp.Value);
						}
						return obj;
					}

				default:
					return null;
			}
		}

		public static TemplateDataAccessor LuaValueToLLTSharp(LuaValue value)
		{
			switch (value)
			{
				case LuaNil:
					return TemplateNullAccessor.Instance;

				case LuaBoolean boolean:
					return new TemplateBooleanAccessor(boolean.Value);

				case LuaNumber number:
					return new TemplateNumberAccessor(number.Value);

				case LuaString str:
					return new TemplateStringAccessor(str.Value);

				case LuaTable table:
					if (IsArray(table))
					{
						var arr = new List<TemplateDataAccessor>();
						for (int i = 1; i <= table.Length; i++)
							arr.Add(LuaValueToLLTSharp(table.Get(i)));
						return new TemplateArrayAccessor(arr);
					}
					else
					{
						var obj = new Dictionary<string, TemplateDataAccessor>();
						foreach (var kvp in table.Entries)
						{
							string? key = KeyToString(kvp.Key);
							if (key != null)
								obj[key] = LuaValueToLLTSharp(kvp.Value);
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
		private static bool IsArray(LuaTable table)
		{
			int len = table.Length;

			if (len == 0 && table.Keys.Count() == 0)
				return true;

			if (len == 0)
				return false;

			for (int i = 1; i <= len; i++)
			{
				if (!table.Keys.Contains(new LuaNumber(i)))
					return false;
			}

			foreach (var key in table.Keys)
			{
				if (key is not LuaNumber numberKey)
					return false;
				double num = numberKey.Value;
				if (num < 1 || num > len || num != Math.Truncate(num))
					return false;
			}

			return true;
		}

		/// <summary>
		/// Converts a Lua key to a JSON object key string.
		/// Returns null for keys that cannot be represented (e.g. table keys).
		/// </summary>
		private static string? KeyToString(LuaValue key)
		{
			switch (key)
			{
				case LuaString str:
					return str.Value;
				case LuaNumber num:
					return num.Value.ToString(CultureInfo.InvariantCulture);
				default:
					return null;
			}
		}
	}
}
