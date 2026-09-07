using System.Diagnostics.CodeAnalysis;
using LLMDesktopAssistant.StructuredValues;
using LLMDesktopAssistant.StructuredValues.Const;

namespace LLMDesktopAssistant.Addons.Parsers.Frontmatter
{
	public class AddonFrontmatterPropertyNodeParser<TNode> : AddonFrontmatterPropertyParser<TNode>
	{
		public override bool TryParse(ConstNodeValue value, ref AddonDiagnostic? diagnostic, bool require, [NotNullWhen(true)] out TNode result)
		{
			if (value is TNode typedValue)
			{
				result = typedValue;
				return true;
			}

			result = default!;
			diagnostic = diagnostic?.Combine(new AddonDiagnostic
			{
				IsFatal = require,
			});
			return false;
		}
	}
}
