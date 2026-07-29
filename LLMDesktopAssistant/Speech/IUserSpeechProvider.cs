namespace LLMDesktopAssistant.Speech
{
	/// <summary>
	/// Interface for user speech provider.
	/// </summary>
	public interface IUserSpeechProvider
	{
		/// <summary>
		/// Event triggered when speech is received, e.g. from a microphone or other input device.
		/// </summary>
		public event Action<string>? OnSpeechReceived;
	}
}