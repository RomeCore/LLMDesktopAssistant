using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using LLMDesktopAssistant.StructuredValues.Const;

namespace LLMDesktopAssistant.Addons.Parsers.Frontmatter
{
	public class AddonFrontmatterStringPropertyParser : AddonFrontmatterPropertyParser<string>
	{
		public static AddonFrontmatterStringPropertyParser Instance { get; } = new();

		public override bool TryParse(ConstNodeValue value, ref AddonDiagnostic? diagnostic, bool require, [NotNullWhen(true)] out string result)
		{
			switch (value)
			{
				case ConstNodeStringValue str:
					result = str.Value ?? string.Empty;
					return true;

				case ConstNodeNumberValue num:
					result = num.Value.ToString(CultureInfo.InvariantCulture);
					return true;

				case ConstNodeBooleanValue boolVal:
					result = boolVal.Value.ToString(CultureInfo.InvariantCulture);
					return true;

				default:
					result = string.Empty;
					diagnostic = AddonDiagnostic.Combine(diagnostic, new AddonDiagnostic
					{
						IsFatal = require,
					});
					return false;
			}
		}
	}
}
