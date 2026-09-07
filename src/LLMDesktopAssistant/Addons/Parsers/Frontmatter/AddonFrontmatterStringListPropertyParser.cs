using System.Diagnostics.CodeAnalysis;
using LLMDesktopAssistant.StructuredValues;
using LLMDesktopAssistant.StructuredValues.Const;

namespace LLMDesktopAssistant.Addons.Parsers.Frontmatter
{
	/// <summary>
	/// Parses a string list frontmatter property, e.g. skill's 'tags' or sub-agent's 'skills'/'sub-agents'.
	/// Accepts both scalar (single item) and array forms.
	/// </summary>
	public class AddonFrontmatterStringListPropertyParser : AddonFrontmatterPropertyParser<ImmutableList<string>>
	{
		public static AddonFrontmatterStringListPropertyParser Instance { get; } = new();

		public override bool TryParse(ConstNodeValue value, ref AddonDiagnostic? diagnostic, bool require, [NotNullWhen(true)] out ImmutableList<string> result)
		{
			switch (value)
			{
				case ConstNodeArrayValue arrayValue:
					var builder = ImmutableList.CreateBuilder<string>();
					foreach (var item in arrayValue.Items)
					{
						var str = item.AsString();
						if (!string.IsNullOrWhiteSpace(str))
							builder.Add(str.Trim());
					}
					result = builder.ToImmutable();
					return true;

				case ConstNodeStringValue strValue when !string.IsNullOrWhiteSpace(strValue.Value):
					result = [strValue.Value.Trim()];
					return true;

				default:
					result = [];
					diagnostic = AddonDiagnostic.Combine(diagnostic, new AddonDiagnostic
					{
						IsFatal = require,
						Messages = ["Expected a string or a list of strings"]
					});
					return false;
			}
		}
	}
}
