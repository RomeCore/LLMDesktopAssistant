using System.Diagnostics.CodeAnalysis;
using LLMDesktopAssistant.StructuredValues.Const;

namespace LLMDesktopAssistant.Addons.Parsers.Frontmatter
{
	public abstract class AddonFrontmatterPropertyParser<T>
	{
		public abstract bool TryParse(ConstNodeValue value, ref AddonDiagnostic? diagnostic, bool require, [NotNullWhen(true)] out T result);
	}
}
