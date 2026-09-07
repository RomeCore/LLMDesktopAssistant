using System.Diagnostics.CodeAnalysis;
using LLMDesktopAssistant.StructuredValues;
using LLMDesktopAssistant.StructuredValues.Const;

namespace LLMDesktopAssistant.Addons.Parsers.Frontmatter
{
	/// <summary>
	/// Parses a string dictionary frontmatter property, e.g. skill's 'metadata' or sub-agent's 'metadata'.
	/// Expects a mapping node; values are converted to strings.
	/// </summary>
	public class AddonFrontmatterStringDictionaryPropertyParser : AddonFrontmatterPropertyParser<ImmutableDictionary<string, string>>
	{
		public static AddonFrontmatterStringDictionaryPropertyParser Instance { get; } = new();

		public override bool TryParse(ConstNodeValue value, ref AddonDiagnostic? diagnostic, bool require, [NotNullWhen(true)] out ImmutableDictionary<string, string> result)
		{
			if (value is ConstNodeDictionaryValue dictValue)
			{
				var builder = ImmutableDictionary.CreateBuilder<string, string>();
				foreach (var (key, item) in dictValue.Items)
				{
					var str = item.AsString();
					if (!string.IsNullOrWhiteSpace(str))
						builder[key] = str.Trim();
				}
				result = builder.ToImmutable();
				return true;
			}

			result = [];
			diagnostic = AddonDiagnostic.Combine(diagnostic, new AddonDiagnostic
			{
				IsFatal = require,
				Messages = ["Expected a dictionary of strings"]
			});
			return false;
		}
	}
}
