using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using MoonSharp.Interpreter;

namespace LLMDesktopAssistant.Scripting.Lua
{
	/// <summary>
	/// Lua API for regular expressions: <c>regex.*</c>.
	/// Registered in the global namespace.
	/// </summary>
	[LuaApi]
	public class LuaApiRegex : LuaApiBase
	{
		public override string? Namespace => "regex";

		public override string? Manuals => """
			--- regex — regular expressions API

			Provides classic regular expression operations using .NET regex syntax.
			All functions accept an optional 'flags' string parameter.

			Available flags (combine as needed, e.g. "im"):
			  i — ignore case
			  m — multiline (^ and $ match line boundaries)
			  s — singleline (. matches newline)
			  x — ignore pattern whitespace
			  c — (replace only) replace only the first occurrence

			FUNCTIONS:

			--- regex.test(pattern, text, [flags])
			  Returns true if the pattern matches anywhere in the text.
			  Parameters:
			    - pattern: string — Regular expression
			    - text: string — Text to search
			    - flags: string (optional) — Regex flags
			  Returns: boolean

			--- regex.match(pattern, text, [flags])
			  Returns the first match with detailed info.
			  Parameters:
			    - pattern: string — Regular expression
			    - text: string — Text to search
			    - flags: string (optional) — Regex flags
			  Returns: table or nil
			  Result fields:
			    - value: string — The matched text
			    - start: number — Start position (1-based)
			    - end: number — End position (1-based, inclusive)
			    - length: number — Match length
			    - groups: table — Group captures. Numeric keys (1..n) and named keys.
			      Each group: { value (string or nil), start, end, length }
			    - group_names: array of strings — Named group names

			--- regex.matches(pattern, text, [flags])
			  Returns all non-overlapping matches.
			  Parameters:
			    - pattern: string — Regular expression
			    - text: string — Text to search
			    - flags: string (optional) — Regex flags
			  Returns: array of match tables (same as regex.match result)

			--- regex.replace(pattern, replacement, text, [flags])
			  Replaces matches with replacement string.
			  Parameters:
			    - pattern: string — Regular expression
			    - replacement: string — Replacement (supports $1, ${name}, $&, $`, $', $_)
			    - text: string — Input text
			    - flags: string (optional) — Regex flags, plus "c" for count=1
			  Returns: string

			--- regex.split(pattern, text, [flags])
			  Splits text by the regex pattern.
			  Parameters:
			    - pattern: string — Regular expression delimiter
			    - text: string — Text to split
			    - flags: string (optional) — Regex flags
			  Returns: array of strings

			--- regex.escape(text)
			  Escapes all regex metacharacters in the text.
			  Parameters:
			    - text: string — Text to escape
			  Returns: string

			EXAMPLES:

			  -- Test
			  if regex.test("hello", "Hello World", "i") then
			    print("Found!")
			  end

			  -- Match with groups
			  local m = regex.match("(\\w+)@(\\w+)", "user@example.com")
			  print(m.groups[1].value) -- "user"

			  -- All matches
			  for _, w in ipairs(regex.matches("\\w+", "Hello World")) do
			    print(w.value)
			  end

			  -- Replace
			  print(regex.replace("\\d+", "[#]", "abc123def456")) -- "abc[#]def[#]"

			  -- Split
			  local parts = regex.split("[,\\s]+", "a, b, c")
			  print(parts[1]) -- "a"

			  -- Escape
			  print(regex.escape("(hello)")) -- "\\(hello\\)"
			""";

		public override void Populate(Table globals, Table ns, LuaService luaService)
		{
			ns["test"] = DynValue.NewCallback(new CallbackFunction(Test));
			ns["match"] = DynValue.NewCallback(new CallbackFunction(Match));
			ns["matches"] = DynValue.NewCallback(new CallbackFunction(Matches));
			ns["replace"] = DynValue.NewCallback(new CallbackFunction(Replace));
			ns["split"] = DynValue.NewCallback(new CallbackFunction(Split));
			ns["escape"] = DynValue.NewCallback(new CallbackFunction(Escape));
		}

		private static DynValue Test(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("regex.test(pattern, text, [flags]): at least 2 arguments expected.");

			var pattern = args[0].CastToString();
			var text = args[1].CastToString();
			if (pattern == null)
				throw new ScriptRuntimeException("regex.test(): first argument must be a string (pattern).");
			if (text == null)
				throw new ScriptRuntimeException("regex.test(): second argument must be a string (text).");

			var flags = ParseFlags(args, 2);
			try
			{
				var regex = new Regex(pattern, flags);
				return DynValue.NewBoolean(regex.IsMatch(text));
			}
			catch (ArgumentException ex)
			{
				throw new ScriptRuntimeException($"Regex error: {ex.Message}");
			}
		}

		private static DynValue Match(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("regex.match(pattern, text, [flags]): at least 2 arguments expected.");

			var pattern = args[0].CastToString();
			var text = args[1].CastToString();
			if (pattern == null)
				throw new ScriptRuntimeException("regex.match(): first argument must be a string (pattern).");
			if (text == null)
				throw new ScriptRuntimeException("regex.match(): second argument must be a string (text).");

			var flags = ParseFlags(args, 2);
			try
			{
				var regex = new Regex(pattern, flags);
				var match = regex.Match(text);
				if (!match.Success)
					return DynValue.Nil;

				return DynValue.NewTable(MatchToTable(ctx.OwnerScript, regex, match));
			}
			catch (ArgumentException ex)
			{
				throw new ScriptRuntimeException($"Regex error: {ex.Message}");
			}
		}

		private static DynValue Matches(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("regex.matches(pattern, text, [flags]): at least 2 arguments expected.");

			var pattern = args[0].CastToString();
			var text = args[1].CastToString();
			if (pattern == null)
				throw new ScriptRuntimeException("regex.matches(): first argument must be a string (pattern).");
			if (text == null)
				throw new ScriptRuntimeException("regex.matches(): second argument must be a string (text).");

			var flags = ParseFlags(args, 2);
			try
			{
				var regex = new Regex(pattern, flags);
				var matches = regex.Matches(text);

				var resultArray = new Table(ctx.OwnerScript);
				for (int i = 0; i < matches.Count; i++)
				{
					resultArray[i + 1] = DynValue.NewTable(MatchToTable(ctx.OwnerScript, regex, matches[i]));
				}
				return DynValue.NewTable(resultArray);
			}
			catch (ArgumentException ex)
			{
				throw new ScriptRuntimeException($"Regex error: {ex.Message}");
			}
		}

		private static DynValue Replace(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 3)
				throw new ScriptRuntimeException("regex.replace(pattern, replacement, text, [flags]): at least 3 arguments expected.");

			var pattern = args[0].CastToString();
			var replacement = args[1].CastToString();
			var text = args[2].CastToString();
			if (pattern == null)
				throw new ScriptRuntimeException("regex.replace(): first argument must be a string (pattern).");
			if (replacement == null)
				throw new ScriptRuntimeException("regex.replace(): second argument must be a string (replacement).");
			if (text == null)
				throw new ScriptRuntimeException("regex.replace(): third argument must be a string (text).");

			var (regexOptions, countOne) = ParseReplaceFlags(args, 3);

			try
			{
				var regex = new Regex(pattern, regexOptions);

				string result;
				if (countOne)
					result = regex.Replace(text, replacement, 1);
				else
					result = regex.Replace(text, replacement);

				return DynValue.NewString(result);
			}
			catch (ArgumentException ex)
			{
				throw new ScriptRuntimeException($"Regex error: {ex.Message}");
			}
		}

		private static DynValue Split(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 2)
				throw new ScriptRuntimeException("regex.split(pattern, text, [flags]): at least 2 arguments expected.");

			var pattern = args[0].CastToString();
			var text = args[1].CastToString();
			if (pattern == null)
				throw new ScriptRuntimeException("regex.split(): first argument must be a string (pattern).");
			if (text == null)
				throw new ScriptRuntimeException("regex.split(): second argument must be a string (text).");

			var flags = ParseFlags(args, 2);
			try
			{
				var regex = new Regex(pattern, flags);
				var parts = regex.Split(text);

				var resultArray = new Table(ctx.OwnerScript);
				for (int i = 0; i < parts.Length; i++)
				{
					resultArray[i + 1] = DynValue.NewString(parts[i]);
				}
				return DynValue.NewTable(resultArray);
			}
			catch (ArgumentException ex)
			{
				throw new ScriptRuntimeException($"Regex error: {ex.Message}");
			}
		}

		private static DynValue Escape(ScriptExecutionContext ctx, CallbackArguments args)
		{
			if (args.Count < 1)
				throw new ScriptRuntimeException("regex.escape(text): at least 1 argument expected.");

			var text = args[0].CastToString();
			if (text == null)
				throw new ScriptRuntimeException("regex.escape(): first argument must be a string (text).");

			try
			{
				return DynValue.NewString(Regex.Escape(text));
			}
			catch (ArgumentException ex)
			{
				throw new ScriptRuntimeException($"Regex error: {ex.Message}");
			}
		}

		/// <summary>
		/// Parses flags string into RegexOptions.
		/// Special flag 'c' uses Compiled bit as a marker for count=1.
		/// </summary>
		private static RegexOptions ParseFlags(CallbackArguments args, int flagsIndex)
		{
			var options = RegexOptions.None;

			if (args.Count > flagsIndex)
			{
				var flagsStr = args[flagsIndex].CastToString();
				if (flagsStr != null)
				{
					foreach (char c in flagsStr)
					{
						switch (c)
						{
							case 'i': options |= RegexOptions.IgnoreCase; break;
							case 'm': options |= RegexOptions.Multiline; break;
							case 's': options |= RegexOptions.Singleline; break;
							case 'x': options |= RegexOptions.IgnorePatternWhitespace; break;
							case 'c': break; // special: count=1
							default:
								throw new ScriptRuntimeException($"Unknown regex flag: '{c}'. Supported: i, m, s, x, c");
						}
					}
				}
			}

			return options;
		}

		/// <summary>
		/// Parses flags string for regex.replace, supporting all regex flags plus 'c' for count=1.
		/// Returns a tuple of (RegexOptions, countOne).
		/// </summary>
		private static (RegexOptions Options, bool CountOne) ParseReplaceFlags(CallbackArguments args, int flagsIndex)
		{
			var options = RegexOptions.None;
			bool countOne = false;

			if (args.Count > flagsIndex)
			{
				var flagsStr = args[flagsIndex].CastToString();
				if (flagsStr != null)
				{
					foreach (char c in flagsStr)
					{
						switch (c)
						{
							case 'i': options |= RegexOptions.IgnoreCase; break;
							case 'm': options |= RegexOptions.Multiline; break;
							case 's': options |= RegexOptions.Singleline; break;
							case 'x': options |= RegexOptions.IgnorePatternWhitespace; break;
							case 'c': countOne = true; break;
							default:
								throw new ScriptRuntimeException($"Unknown regex flag: '{c}'. Supported: i, m, s, x, c");
						}
					}
				}
			}

			return (options, countOne);
		}


		/// <summary>
		/// Converts a .NET Match object into a Lua table with detailed info.
		/// </summary>
		private static Table MatchToTable(Script script, Regex regex, Match match)
		{
			var t = new Table(script);

			t["value"] = DynValue.NewString(match.Value);
			t["start"] = DynValue.NewNumber(match.Index + 1); // Lua is 1-based
			t["end"] = DynValue.NewNumber(match.Index + match.Length); // inclusive
			t["length"] = DynValue.NewNumber(match.Length);

			// Group names list
			var groupNamesTable = new Table(script);
			var groupNames = regex.GetGroupNames();
			int nameIdx = 1;
			foreach (var name in groupNames)
			{
				// skip numeric names (they are just indices)
				if (!int.TryParse(name, out _))
				{
					groupNamesTable[nameIdx] = DynValue.NewString(name);
					nameIdx++;
				}
			}
			if (nameIdx == 1)
				t["group_names"] = DynValue.NewTable(new Table(script)); // empty table
			else
				t["group_names"] = DynValue.NewTable(groupNamesTable);

			// Groups
			var groupsTable = new Table(script);
			foreach (var name in groupNames)
			{
				var group = match.Groups[name];
				var groupTable = new Table(script);

				if (group.Success)
				{
					groupTable["value"] = DynValue.NewString(group.Value);
					groupTable["start"] = DynValue.NewNumber(group.Index + 1);
					groupTable["end"] = DynValue.NewNumber(group.Index + group.Length);
					groupTable["length"] = DynValue.NewNumber(group.Length);
				}
				else
				{
					groupTable["value"] = DynValue.Nil;
					groupTable["start"] = DynValue.Nil;
					groupTable["end"] = DynValue.Nil;
					groupTable["length"] = DynValue.NewNumber(0);
				}

				// Use numeric key if name is a number, otherwise string key
				if (int.TryParse(name, out int numericKey))
					groupsTable[numericKey] = DynValue.NewTable(groupTable);
				else
					groupsTable[name] = DynValue.NewTable(groupTable);
			}
			t["groups"] = DynValue.NewTable(groupsTable);

			return t;
		}
	}
}
