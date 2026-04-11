namespace LLMDesktopAssistant.Core.Utils
{
	public static class StringExtensions
	{
		public static string? ToNullIfEmpty(this string? value)
		{
			if (string.IsNullOrEmpty(value))
				return null;
			return value;
		}

		public static string? ToNullIfWhiteSpace(this string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return null;
			return value;
		}
	}
}