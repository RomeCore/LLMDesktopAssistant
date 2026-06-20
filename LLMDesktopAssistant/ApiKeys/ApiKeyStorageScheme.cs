namespace LLMDesktopAssistant.ApiKeys
{
	public enum ApiKeyStorageScheme
	{
		/// <summary>
		/// Stores the API key in a raw format, without any encryption or obfuscation.
		/// </summary>
		Raw = 0,

		/// <summary>
		/// Stores the API key in an encrypted format.
		/// </summary>
		Encrypted = 1,

		/// <summary>
		/// Stores the API key in an environment variable.
		/// </summary>
		EnvironmentVariable = 2,
	}
}
