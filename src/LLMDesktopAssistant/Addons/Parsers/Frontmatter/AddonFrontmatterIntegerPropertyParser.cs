using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using LLMDesktopAssistant.StructuredValues.Const;

namespace LLMDesktopAssistant.Addons.Parsers.Frontmatter
{
	/// <summary>
	/// Parses an integer frontmatter property.
	/// Accepts numeric nodes and string nodes with an invariant representation.
	/// </summary>
	public class AddonFrontmatterIntegerPropertyParser : AddonFrontmatterPropertyParser<int>
	{
		public static AddonFrontmatterIntegerPropertyParser Instance { get; } = new();

		public override bool TryParse(ConstNodeValue value, ref AddonDiagnostic? diagnostic, bool require, [NotNullWhen(true)] out int result)
		{
			switch (value)
			{
				case ConstNodeNumberValue numValue
					when numValue.Value == Math.Truncate(numValue.Value)
						&& numValue.Value >= int.MinValue
						&& numValue.Value <= int.MaxValue:
					result = (int)numValue.Value;
					return true;

				case ConstNodeStringValue strValue when int.TryParse(strValue.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed):
					result = parsed;
					return true;

				default:
					result = 0;
					diagnostic = AddonDiagnostic.Combine(diagnostic, new AddonDiagnostic
					{
						IsFatal = require,
						Messages = ["Expected an integer value"]
					});
					return false;
			}
		}
	}
}
