using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API with extended string utilities: <c>string.*</c>.
	/// Supplements the built-in string library with commonly needed functions.
	/// </summary>
	[LuaApi]
	public class LuaApiString : LuaApiBase
	{
		public override string? Namespace => "string";

		public override string? Manuals => """
			--- string — extended string utilities

			Supplements the built-in Lua string library.

			FUNCTIONS:

			--- string.split(str, delimiter)
			  Splits a string by a delimiter.
			  Parameters:
			    - str: string — string to split
			    - delimiter: string or pattern (optional, default "%s+") — delimiter pattern
			  Returns: table — array of substrings

			--- string.trim(str)
			--- string.trim_left(str)
			--- string.trim_right(str)
			  Removes whitespace from both ends (or one side) of a string.

			--- string.startswith(str, prefix)
			--- string.endswith(str, suffix)
			  Checks if a string starts/ends with a given substring.
			  Returns: boolean

			--- string.contains(str, substr)
			  Checks if a string contains a substring.
			  Returns: boolean

			--- string.slug(str)
			  Converts a string to a URL-friendly slug.
			  Replaces spaces with hyphens, removes special chars,
			  converts to lowercase, handles basic transliteration.
			  Parameters:
			    - str: string — input text
			  Returns: string — e.g. "Hello World!" -> "hello-world"

			--- string.wrap(str, width, [indent])
			  Wraps text to a specified line width.
			  Parameters:
			    - str: string — text to wrap
			    - width: number — maximum line width (default: 80)
			    - indent: string (optional) — prefix for each line (e.g. "> ")
			  Returns: string — wrapped text

			--- string.truncate(str, max_len, [suffix])
			  Truncates a string to a maximum length, appending a suffix if trimmed.
			  Parameters:
			    - str: string — input text
			    - max_len: number — maximum length
			    - suffix: string (optional, default "...") — suffix to append
			  Returns: string

			--- string.pad_left(str, total_width, [char])
			--- string.pad_right(str, total_width, [char])
			  Pads a string to a given width with a character.
			  Parameters:
			    - str: string — input string
			    - total_width: number — target length
			    - char: string (optional, default " ") — padding character
			  Returns: string

			--- string.lines(str)
			  Splits a string into lines (handles \\n, \\r\\n, \\r).
			  Returns: table — array of strings

			--- string.upper_first(str)
			  Capitalizes the first character of a string.
			  Parameters:
			    - str: string — input text
			  Returns: string — e.g. "hello" -> "Hello"

			EXAMPLES:

			  local parts = string.split("a,b,c", ",")
			  print(string.trim("  hello  "))
			  if string.startswith("hello", "he") then end
			  print(string.slug("Hello World!")) -- "hello-world"
			  print(string.wrap("long text...", 40))
			  print(string.truncate("long text", 6)) -- "long t..."
			""";

		public override void Populate(Table globals, Table ns)
		{
			ns["split"] = DynValue.NewCallback(new CallbackFunction(Split));
			ns["trim"] = DynValue.NewCallback(new CallbackFunction(Trim));
			ns["trim_left"] = DynValue.NewCallback(new CallbackFunction(TrimLeft));
			ns["trim_right"] = DynValue.NewCallback(new CallbackFunction(TrimRight));
			ns["startswith"] = DynValue.NewCallback(new CallbackFunction(StartsWith));
			ns["endswith"] = DynValue.NewCallback(new CallbackFunction(EndsWith));
			ns["contains"] = DynValue.NewCallback(new CallbackFunction(Contains));
			ns["slug"] = DynValue.NewCallback(new CallbackFunction(Slug));
			ns["wrap"] = DynValue.NewCallback(new CallbackFunction(Wrap));
			ns["truncate"] = DynValue.NewCallback(new CallbackFunction(Truncate));
			ns["pad_left"] = DynValue.NewCallback(new CallbackFunction(PadLeft));
			ns["pad_right"] = DynValue.NewCallback(new CallbackFunction(PadRight));
			ns["lines"] = DynValue.NewCallback(new CallbackFunction(Lines));
			ns["upper_first"] = DynValue.NewCallback(new CallbackFunction(UpperFirst));
		}

		private static DynValue Split(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("string.split(str, [delimiter]): at least 1 argument expected.");
			var str = args[0].CastToString();
			if (str == null)
				throw new ScriptRuntimeException("string.split(): first argument must be a string.");

			string delimiter = "%s+"; // Lua pattern by default
			if (args.Count > 1 && !args[1].IsNil())
			{
				var d = args[1].CastToString();
				if (d == null)
					throw new ScriptRuntimeException("string.split(): delimiter must be a string.");
				delimiter = d;
			}

			// Escape Lua magic characters for literal splitting,
			// but allow Lua patterns if explicitly using % patterns
			// For literal strings, escape special chars
			string pattern;
			if (delimiter.Contains('%'))
				pattern = delimiter; // user wants Lua pattern
			else
				pattern = EscapeLuaPattern(delimiter); // literal split

			var result = new Table(ctx.OwnerScript);
			// Use Lua string.gmatch via MoonSharp
			var script = ctx.OwnerScript;
			var gmatchCode = $@"
local result = {{}}
for s in string.gmatch({EscapeLuaString(str)}, {EscapeLuaString(pattern)}) do
    result[#result + 1] = s
end
return result
";
			// Simpler approach: manual split
			var parts = SplitByPattern(str, pattern);
			for (int i = 0; i < parts.Length; i++)
				result[i + 1] = DynValue.NewString(parts[i]);

			return DynValue.NewTable(result);
		}

		private static DynValue Trim(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("string.trim(str): at least 1 argument expected.");
			var str = args[0].CastToString();
			if (str == null)
				throw new ScriptRuntimeException("string.trim(): first argument must be a string.");
			return DynValue.NewString(str.Trim());
		}

		private static DynValue TrimLeft(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("string.trim_left(str): at least 1 argument expected.");
			var str = args[0].CastToString();
			if (str == null)
				throw new ScriptRuntimeException("string.trim_left(): first argument must be a string.");
			return DynValue.NewString(str.TrimStart());
		}

		private static DynValue TrimRight(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("string.trim_right(str): at least 1 argument expected.");
			var str = args[0].CastToString();
			if (str == null)
				throw new ScriptRuntimeException("string.trim_right(): first argument must be a string.");
			return DynValue.NewString(str.TrimEnd());
		}

		private static DynValue StartsWith(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("string.startswith(str, prefix): at least 2 arguments expected.");
			var str = args[0].CastToString();
			var prefix = args[1].CastToString();
			if (str == null || prefix == null)
				throw new ScriptRuntimeException("string.startswith(): both arguments must be strings.");
			return DynValue.NewBoolean(str.StartsWith(prefix));
		}

		private static DynValue EndsWith(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("string.endswith(str, suffix): at least 2 arguments expected.");
			var str = args[0].CastToString();
			var suffix = args[1].CastToString();
			if (str == null || suffix == null)
				throw new ScriptRuntimeException("string.endswith(): both arguments must be strings.");
			return DynValue.NewBoolean(str.EndsWith(suffix));
		}

		private static DynValue Contains(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("string.contains(str, substr): at least 2 arguments expected.");
			var str = args[0].CastToString();
			var substr = args[1].CastToString();
			if (str == null || substr == null)
				throw new ScriptRuntimeException("string.contains(): both arguments must be strings.");
			return DynValue.NewBoolean(str.Contains(substr));
		}

		private static DynValue Slug(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("string.slug(str): at least 1 argument expected.");
			var str = args[0].CastToString();
			if (str == null)
				throw new ScriptRuntimeException("string.slug(): first argument must be a string.");

			// Transliterate common chars, then slugify
			var slug = str.ToLowerInvariant();
			slug = RemoveDiacritics(slug);
			var sb = new StringBuilder();
			foreach (char c in slug)
			{
				if (char.IsLetterOrDigit(c))
					sb.Append(c);
				else if (c == ' ' || c == '-' || c == '_' || c == '.' || c == '~')
					sb.Append('-');
			}
			// Collapse multiple hyphens
			var result = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), "-{2,}", "-");
			// Trim hyphens from edges
			result = result.Trim('-');
			return DynValue.NewString(result);
		}

		private static DynValue Wrap(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("string.wrap(str, [width], [indent]): at least 1 argument expected.");
			var str = args[0].CastToString();
			if (str == null)
				throw new ScriptRuntimeException("string.wrap(): first argument must be a string.");

			int width = 80;
			if (args.Count > 1 && !args[1].IsNil())
			{
				var w = args[1].CastToNumber();
				if (w == null)
					throw new ScriptRuntimeException("string.wrap(): width must be a number.");
				width = (int)w.Value;
				if (width < 1)
					throw new ScriptRuntimeException("string.wrap(): width must be positive.");
			}

			string indent = "";
			if (args.Count > 2 && !args[2].IsNil())
			{
				var ind = args[2].CastToString();
				if (ind == null)
					throw new ScriptRuntimeException("string.wrap(): indent must be a string.");
				indent = ind;
			}

			var words = str.Split(' ', StringSplitOptions.RemoveEmptyEntries);
			var result = new StringBuilder();
			int lineLen = indent.Length;

			foreach (var word in words)
			{
				if (lineLen + word.Length > width && lineLen > indent.Length)
				{
					result.AppendLine();
					result.Append(indent);
					lineLen = indent.Length;
				}
				else if (lineLen > indent.Length)
				{
					result.Append(' ');
					lineLen++;
				}

				result.Append(word);
				lineLen += word.Length;
			}

			return DynValue.NewString(result.ToString());
		}

		private static DynValue Truncate(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("string.truncate(str, max_len, [suffix]): at least 2 arguments expected.");
			var str = args[0].CastToString();
			var maxLenVal = args[1].CastToNumber();
			if (str == null || maxLenVal == null)
				throw new ScriptRuntimeException("string.truncate(): first two arguments must be string and number.");
			int maxLen = (int)maxLenVal.Value;

			string suffix = "...";
			if (args.Count > 2 && !args[2].IsNil())
			{
				var s = args[2].CastToString();
				if (s == null)
					throw new ScriptRuntimeException("string.truncate(): suffix must be a string.");
				suffix = s;
			}

			if (str.Length <= maxLen)
				return DynValue.NewString(str);

			if (maxLen <= suffix.Length)
				return DynValue.NewString(str.Substring(0, maxLen));

			return DynValue.NewString(str.Substring(0, maxLen - suffix.Length) + suffix);
		}

		private static DynValue PadLeft(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("string.pad_left(str, total_width, [char]): at least 2 arguments expected.");
			var str = args[0].CastToString();
			var widthVal = args[1].CastToNumber();
			if (str == null || widthVal == null)
				throw new ScriptRuntimeException("string.pad_left(): first two arguments must be string and number.");
			int width = (int)widthVal.Value;

			char padChar = ' ';
			if (args.Count > 2 && !args[2].IsNil())
			{
				var pc = args[2].CastToString();
				if (pc == null || pc.Length == 0)
					throw new ScriptRuntimeException("string.pad_left(): pad char must be a non-empty string.");
				padChar = pc[0];
			}

			return DynValue.NewString(str.PadLeft(width, padChar));
		}

		private static DynValue PadRight(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("string.pad_right(str, total_width, [char]): at least 2 arguments expected.");
			var str = args[0].CastToString();
			var widthVal = args[1].CastToNumber();
			if (str == null || widthVal == null)
				throw new ScriptRuntimeException("string.pad_right(): first two arguments must be string and number.");
			int width = (int)widthVal.Value;

			char padChar = ' ';
			if (args.Count > 2 && !args[2].IsNil())
			{
				var pc = args[2].CastToString();
				if (pc == null || pc.Length == 0)
					throw new ScriptRuntimeException("string.pad_right(): pad char must be a non-empty string.");
				padChar = pc[0];
			}

			return DynValue.NewString(str.PadRight(width, padChar));
		}

		private static DynValue Lines(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("string.lines(str): at least 1 argument expected.");
			var str = args[0].CastToString();
			if (str == null)
				throw new ScriptRuntimeException("string.lines(): first argument must be a string.");

			var lines = str.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
			var result = new Table(ctx.OwnerScript);
			for (int i = 0; i < lines.Length; i++)
				result[i + 1] = DynValue.NewString(lines[i]);
			return DynValue.NewTable(result);
		}

		private static DynValue UpperFirst(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("string.upper_first(str): at least 1 argument expected.");
			var str = args[0].CastToString();
			if (str == null)
				throw new ScriptRuntimeException("string.upper_first(): first argument must be a string.");

			if (string.IsNullOrEmpty(str))
				return DynValue.NewString(str);

			return DynValue.NewString(char.ToUpperInvariant(str[0]) + str.Substring(1));
		}

		// --- Helpers ---

		private static string EscapeLuaPattern(string s)
		{
			// Escape Lua magic characters: ^ $ ( ) % . [ ] * + - ?
			return s.Replace("\\", "\\\\")
					.Replace("^", "\\^")
					.Replace("$", "\\$")
					.Replace("(", "\\(")
					.Replace(")", "\\)")
					.Replace("%", "\\%")
					.Replace(".", "\\.")
					.Replace("[", "\\[")
					.Replace("]", "\\]")
					.Replace("*", "\\*")
					.Replace("+", "\\+")
					.Replace("-", "\\-")
					.Replace("?", "\\?");
		}

		private static string EscapeLuaString(string s)
		{
			// Escape a string for Lua code generation
			return "\"" + s.Replace("\\", "\\\\")
						   .Replace("\"", "\\\"")
						   .Replace("\n", "\\n")
						   .Replace("\r", "\\r")
						   .Replace("\t", "\\t") + "\"";
		}

		private static string[] SplitByPattern(string str, string pattern)
		{
			// Simple split by literal string (not Lua pattern)
			if (pattern.StartsWith("\\"))
			{
				// Remove escaping for simple matching
				var unescaped = pattern.Replace("\\", "");
				if (string.IsNullOrEmpty(unescaped))
					return [str];
				return str.Split(new[] { unescaped }, StringSplitOptions.None);
			}

			// For Lua patterns, fall back to regex approximation
			// This is a simplification — for full Lua pattern support,
			// we'd need to call Lua engine
			if (pattern == "%s+")
				return str.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

			// Default: split by literal
			return str.Split(new[] { pattern }, StringSplitOptions.None);
		}

		private static string RemoveDiacritics(string text)
		{
			var formD = text.Normalize(NormalizationForm.FormD);
			var sb = new StringBuilder();
			foreach (char c in formD)
			{
				var uc = CharUnicodeInfo.GetUnicodeCategory(c);
				if (uc != UnicodeCategory.NonSpacingMark)
					sb.Append(c);
			}
			return sb.ToString().Normalize(NormalizationForm.FormC);
		}
	}
}
