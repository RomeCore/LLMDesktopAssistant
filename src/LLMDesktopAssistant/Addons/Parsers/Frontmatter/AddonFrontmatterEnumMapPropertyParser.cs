using System.Diagnostics.CodeAnalysis;
using LLMDesktopAssistant.StructuredValues;
using LLMDesktopAssistant.StructuredValues.Const;

namespace LLMDesktopAssistant.Addons.Parsers.Frontmatter
{
	/// <summary>
	/// Parses an enum dictionary frontmatter property, e.g. sub-agent's 'memory-blocks'
	/// (block name → attachment mode). Accepts a mapping ('block-name: read-only'),
	/// a list of keys (all get <c>default(TEnum)</c>), or a single key.
	/// </summary>
	public class AddonFrontmatterEnumMapPropertyParser<TEnum> : AddonFrontmatterPropertyParser<ImmutableDictionary<string, TEnum>>
		where TEnum : struct, Enum
	{
		public override bool TryParse(ConstNodeValue value, ref AddonDiagnostic? diagnostic, bool require, [NotNullWhen(true)] out ImmutableDictionary<string, TEnum> result)
		{
			var builder = ImmutableDictionary.CreateBuilder<string, TEnum>();

			switch (value)
			{
				case ConstNodeDictionaryValue dictValue:
					foreach (var (key, item) in dictValue.Items)
					{
						var str = item.AsString();
						if (string.IsNullOrWhiteSpace(str))
						{
							builder[key] = default;
							continue;
						}

						builder[key] = AddonFrontmatterEnumParserHelper.TryParseEnum<TEnum>(str, out var mode)
							? mode
							: default;
					}
					result = builder.ToImmutable();
					return true;

				case ConstNodeArrayValue arrayValue:
					foreach (var item in arrayValue.Items)
					{
						var str = item.AsString();
						if (!string.IsNullOrWhiteSpace(str))
							builder[str.Trim()] = default;
					}
					result = builder.ToImmutable();
					return true;

				case ConstNodeStringValue strValue when !string.IsNullOrWhiteSpace(strValue.Value):
					builder[strValue.Value.Trim()] = default;
					result = builder.ToImmutable();
					return true;

				default:
					result = [];
					diagnostic = AddonDiagnostic.Combine(diagnostic, new AddonDiagnostic
					{
						IsFatal = require,
						Messages = [$"Expected a dictionary of '{typeof(TEnum).Name}' values, a list of keys, or a single key"]
					});
					return false;
			}
		}
	}
}
