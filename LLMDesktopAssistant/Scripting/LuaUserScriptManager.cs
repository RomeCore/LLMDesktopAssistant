using System;
using System.Collections.Generic;
using System.Text;
using LLMDesktopAssistant.Scripting.Lua;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;
using RCParsing;

namespace LLMDesktopAssistant.Scripting
{
	[Service(typeof(ILuaUserScriptManager))]
	public class LuaUserScriptManager : ILuaUserScriptManager
	{
		private static readonly Parser _metaParser;

		static LuaUserScriptManager()
		{
			var builder = new ParserBuilder();

			builder.Settings.Skip(
				b => b.Whitespaces(),
				ParserSkippingStrategy.TryParseThenSkip);

			builder.CreateRule("namespace")
				.Literal("--")
				.Literal("NAMESPACE:")
				.ZeroOrMoreSeparated(
					b => b.Identifier(),
					s => s.Literal('.'),
					includeSeparatorsInResult: false)
					.TransformLast(v =>
					{
						return string.Join('.', v.Children.Select(v => v.Text));
					})
				.Newline()
				.TransformSelect(index: 2);

			builder.CreateRule("manuals")
				.Literal("--[[")
				.Literal("MANUALS")
				.Newline()
				.TextUntil("]]")
				.Literal("]]")
				.Transform(v =>
				{
					return v[3].Span.Trim().ToString();
				});

			builder.CreateMainRule()
				.Rule("namespace")
				.Rule("manuals")
				.AllText()
				.Transform(v =>
				{
					return (v[0].GetValue<string>(), v[1].GetValue<string>(), v[2].Text);
				});

			_metaParser = builder.Build();
		}

		public event EventHandler? ScriptsChanged;

		private static LuaApiLoadedScript CreateApi(string? scriptPath, string? rawScript = null)
		{
			if (scriptPath == null && rawScript == null)
				throw new ArgumentNullException(nameof(scriptPath), "Either scriptPath or rawScript must be provided.");

			var fullScriptPath = scriptPath != null ? Path.Combine(Directories.LuaScripts, scriptPath) : null;
			if (fullScriptPath != null && !File.Exists(fullScriptPath))
				throw new FileNotFoundException($"The specified script file does not exist: {fullScriptPath}", fullScriptPath);

			if (fullScriptPath != null)
				rawScript = File.ReadAllText(fullScriptPath);

			var (ns, manuals, script) = _metaParser.Parse<(string, string, string)>(rawScript!);
			return new LuaApiLoadedScript(scriptPath, ns, manuals, script);
		}

		public static string NormalizeScriptPath(string path)
		{
			var dirName = Path.GetDirectoryName(path) ?? string.Empty;
			var combined = Path.Combine(dirName, Path.GetFileNameWithoutExtension(path) + ".lua");
			return combined;
		}

		public IEnumerable<LuaApiLoadedScript> GetScripts()
		{
			var files = Directory.GetFiles(Directories.LuaScripts, "*.lua", SearchOption.AllDirectories);
			return files.Select(f => CreateApi(Path.GetRelativePath(Directories.LuaScripts, f), f)).ToList();
		}

		public void RegisterOrUpdateScript(string path, string? ns, string? manuals, string? script)
		{
			path = NormalizeScriptPath(path);
			var fullPath = Path.Combine(Directories.LuaScripts, path);

			if (File.Exists(fullPath))
			{
				var existingRawScript = File.ReadAllText(fullPath);
				var (existingNs, existingManuals, existingScript) = _metaParser.Parse<(string, string, string)>(existingRawScript);
				ns ??= existingNs;
				manuals ??= existingManuals;
				script ??= existingScript;
			}
			else
			{
				List<string> nullArgs = [];

				if (ns == null) nullArgs.Add(nameof(ns));
				if (manuals == null) nullArgs.Add(nameof(manuals));
				if (script == null) nullArgs.Add(nameof(script));

				if (nullArgs.Count != 0)
					throw new ArgumentNullException(string.Join(", ", nullArgs.ToArray()),
						"All parameters must be provided if the script does not exist.");
			}

			string combinedScript = $"""
				-- NAMESPACE: {ns}
				--[[ MANUALS
				{manuals}
				]]
				{script}
				""";

			var directoryName = Path.GetDirectoryName(fullPath);
			if (directoryName != null)
				Directory.CreateDirectory(directoryName);
			File.WriteAllText(fullPath, combinedScript);

			ScriptsChanged?.Invoke(this, EventArgs.Empty);
		}

		public bool RemoveScript(string path)
		{
			path = NormalizeScriptPath(path);
			var fullPath = Path.Combine(Directories.LuaScripts, path);

			if (File.Exists(fullPath))
			{
				File.Delete(fullPath);
				ScriptsChanged?.Invoke(this, EventArgs.Empty);
				return true;
			}
			return true;
		}

		public bool MoveScript(string oldPath, string newPath)
		{
			oldPath = NormalizeScriptPath(oldPath);
			newPath = NormalizeScriptPath(newPath);

			var fullOldPath = Path.Combine(Directories.LuaScripts, oldPath);
			var fullNewPath = Path.Combine(Directories.LuaScripts, newPath);

			if (File.Exists(fullOldPath) && !File.Exists(fullNewPath))
			{
				var directoryName = Path.GetDirectoryName(fullNewPath);
				if (directoryName != null)
					Directory.CreateDirectory(directoryName);
				File.Move(fullOldPath, fullNewPath);
				ScriptsChanged?.Invoke(this, EventArgs.Empty);
				return true;
			}
			return false;
		}
	}
}