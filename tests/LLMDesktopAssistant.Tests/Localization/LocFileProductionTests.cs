using System.Reflection;
using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Tests.Localization;

public class LocFileProductionTests
{
	private static readonly Assembly CoreAssembly = typeof(Locale).Assembly;

	private static IEnumerable<string> GetProductionLocFiles()
	{
		return CoreAssembly.GetManifestResourceNames()
			.Where(n => n.EndsWith(".loc", StringComparison.OrdinalIgnoreCase));
	}

	[Fact]
	public void Parse_AllProductionLocFiles()
	{
		var files = GetProductionLocFiles().ToArray();

		Assert.NotEmpty(files);

		foreach (var name in files)
		{
			using var stream = CoreAssembly.GetManifestResourceStream(name);
			Assert.NotNull(stream);

			using var reader = new StreamReader(stream);
			var document = LocFileParser.Parse(reader.ReadToEnd());

			Assert.NotEmpty(document.Entries);
		}
	}

	[Fact]
	public void Keys_AreUniqueAcrossFilesOfSameLocale()
	{
		var keysByLocale = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

		foreach (var name in GetProductionLocFiles())
		{
			using var stream = CoreAssembly.GetManifestResourceStream(name);
			Assert.NotNull(stream);

			using var reader = new StreamReader(stream);
			var document = LocFileParser.Parse(reader.ReadToEnd());

			if (!keysByLocale.TryGetValue(document.Locale, out var keys))
			{
				keys = new HashSet<string>(StringComparer.Ordinal);
				keysByLocale[document.Locale] = keys;
			}

			foreach (var key in document.Entries.Keys)
			{
				Assert.True(
					keys.Add(key),
					$"Duplicate key '{key}' across .loc files of locale '{document.Locale}'.");
			}
		}
	}
}
