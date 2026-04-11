namespace LLMDesktopAssistant.Core.Localization
{
	/// <summary>
	/// Extension methods for localization.
	/// </summary>
	public static class LocalizationExtensions
	{
		/// <summary>
		/// Extension method to localize a string.
		/// </summary>
		/// <param name="key">The key to localize.</param>
		/// <returns>Localized string. If the key is not found, returns the original key.</returns>
		public static string Localize(this string key) => LocalizationManager.LocalizeStatic(key);
	}
}