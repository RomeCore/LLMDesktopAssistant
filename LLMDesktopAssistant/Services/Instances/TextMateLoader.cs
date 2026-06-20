using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using LiveMarkdown.Avalonia;
using LLMDesktopAssistant.Markdown;
using Serilog;
using TextMateSharp.Grammars;

namespace LLMDesktopAssistant.Services.Instances
{
	[Service]
	public class TextMateLoader
	{
		public void LoadGrammarsInto(RegistryOptions registry)
		{
			try
			{
				var tempDir = ExtractTextMateResources();
				if (tempDir == null) return;

				registry.LoadFromLocalDir(tempDir, overwrite: true);

				Log.Information("[TextMateLoader] All grammars loaded successfully from {Dir}", tempDir);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "[TextMateLoader] Failed to load grammars");
			}
		}

		private static readonly string TextMatePrefix = $"{typeof(MarkdownCodeBlockHighlightConfigurator).Assembly.GetName().Name}.Assets.TextMate.";

		private static string? ExtractTextMateResources()
		{
			var assembly = typeof(MarkdownCodeBlockHighlightConfigurator).Assembly;

			var allResourceNames = assembly.GetManifestResourceNames();
			var textMateResources = allResourceNames
				.Where(r => r.StartsWith(TextMatePrefix, StringComparison.OrdinalIgnoreCase))
				.ToArray();

			if (textMateResources.Length == 0)
			{
				Log.Warning("[TextMateLoader] No embedded TextMate resources found (prefix: {Prefix})", TextMatePrefix);
				Log.Information("[TextMateLoader] Available resources: {Res}", string.Join(", ", allResourceNames));
				return null;
			}

			var tempRoot = Path.Combine(Path.GetTempPath(), "LLMDesktopAssistant", "TextMate");

			var assemblyVersion = assembly.GetName().Version?.ToString() ?? "1.0";
			var versionFile = Path.Combine(tempRoot, ".version");
			var needExtract = true;

			if (File.Exists(versionFile) && File.ReadAllText(versionFile) == assemblyVersion)
			{
				needExtract = textMateResources.Any(r =>
				{
					var relPath = ConvertResourceNameToRelativePath(r);
					return relPath == null || !File.Exists(Path.Combine(tempRoot, relPath));
				});
			}

			if (needExtract)
			{
				if (Directory.Exists(tempRoot))
					Directory.Delete(tempRoot, recursive: true);
				Directory.CreateDirectory(tempRoot);

				foreach (var resourceName in textMateResources)
				{
					var relativePath = ConvertResourceNameToRelativePath(resourceName);
					if (relativePath == null) continue;

					var targetPath = Path.Combine(tempRoot, relativePath);
					var dir = Path.GetDirectoryName(targetPath);
					if (dir != null && !Directory.Exists(dir))
						Directory.CreateDirectory(dir);

					using var stream = assembly.GetManifestResourceStream(resourceName);
					if (stream == null)
					{
						Log.Warning("[TextMateLoader] Resource not found: {Res}", resourceName);
						continue;
					}

					using var fileStream = File.Create(targetPath);
					stream.CopyTo(fileStream);
				}

				File.WriteAllText(versionFile, assemblyVersion);
				Log.Information("[TextMateLoader] Extracted {Count} grammar resources to {Dir}",
					textMateResources.Length, tempRoot);
			}

			return tempRoot;
		}

		private static string? ConvertResourceNameToRelativePath(string resourceName)
		{
			if (!resourceName.StartsWith(TextMatePrefix, StringComparison.OrdinalIgnoreCase))
				return null;

			var relative = resourceName.Substring(TextMatePrefix.Length);

			var lastDot = relative.LastIndexOf('.');
			if (lastDot < 0) return relative;

			var fileName = relative.Substring(0, lastDot);
			var extension = relative.Substring(lastDot);

			var filePath = fileName.Replace('.', Path.DirectorySeparatorChar);

			return filePath + extension;
		}
	}
}