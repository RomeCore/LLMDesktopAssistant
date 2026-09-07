namespace LLMDesktopAssistant.Addons.Parsers.Frontmatter
{
	/// <summary>
	/// Helper for parsing enum values in frontmatter fields.
	/// Supports both kebab-case ('read-only', 'policy-based') and plain C# enum names case-insensitively.
	/// </summary>
	internal static class AddonFrontmatterEnumParserHelper
	{
		public static bool TryParseEnum<TEnum>(string text, out TEnum result)
			where TEnum : struct, Enum
		{
			var normalized = text.Trim().ToLowerInvariant().Replace('_', '-');

			foreach (var name in Enum.GetNames<TEnum>())
			{
				if (name.ToLowerInvariant().Replace('_', '-') == normalized)
				{
					result = (TEnum)Enum.Parse(typeof(TEnum), name);
					return true;
				}
			}

			return Enum.TryParse(text.Trim(), ignoreCase: true, out result);
		}

		public static bool TryParseFlags<TEnum>(string text, out TEnum result)
			where TEnum : struct, Enum
		{
			var flagsValue = 0L;
			foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
			{
				if (!TryParseEnum<TEnum>(part, out var flag))
				{
					result = default;
					return false;
				}

				flagsValue |= Convert.ToInt64(flag, System.Globalization.CultureInfo.InvariantCulture);
			}

			result = (TEnum)Enum.ToObject(typeof(TEnum), flagsValue);
			return true;
		}
	}
}
