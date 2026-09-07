using System.Diagnostics.CodeAnalysis;
using LLMDesktopAssistant.StructuredValues;
using LLMDesktopAssistant.StructuredValues.Const;
using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.Addons.Parsers.Frontmatter
{
	/// <summary>
	/// Parses a tool list frontmatter property, e.g. 'allowed-tools', 'available-tools' or 'disallowed-tools'
	/// in skills and sub-agents. Accepts both scalar ('tool1(spec), tool2') and array forms.
	/// Uses <see cref="ToolNameWithSpecifierParser"/> to match tool names with optional specifiers.
	/// </summary>
	public class AddonFrontmatterToolListPropertyParser : AddonFrontmatterPropertyParser<ImmutableList<ToolNameWithSpecifier>>
	{
		public static AddonFrontmatterToolListPropertyParser Instance { get; } = new();

		public override bool TryParse(ConstNodeValue value, ref AddonDiagnostic? diagnostic, bool require, [NotNullWhen(true)] out ImmutableList<ToolNameWithSpecifier> result)
		{
			var builder = ImmutableList.CreateBuilder<ToolNameWithSpecifier>();

			switch (value)
			{
				case ConstNodeArrayValue arrayValue:
					foreach (var item in arrayValue.Items)
					{
						var str = item.AsString();
						if (!string.IsNullOrWhiteSpace(str))
							builder.AddRange(ToolNameWithSpecifierParser.FindAllMatches(str));
					}
					result = builder.ToImmutable();
					return true;

				case ConstNodeStringValue strValue when !string.IsNullOrWhiteSpace(strValue.Value):
					builder.AddRange(ToolNameWithSpecifierParser.FindAllMatches(strValue.Value));
					result = builder.ToImmutable();
					return true;

				default:
					result = [];
					diagnostic = AddonDiagnostic.Combine(diagnostic, new AddonDiagnostic
					{
						IsFatal = require,
						Messages = ["Expected a tool name or a list of tool names"]
					});
					return false;
			}
		}
	}
}
