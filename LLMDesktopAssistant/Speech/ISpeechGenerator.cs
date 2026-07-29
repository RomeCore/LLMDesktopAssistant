namespace LLMDesktopAssistant.Speech
{
	/// <summary>
	/// Interface for speech generation from text.
	/// </summary>
	public interface ISpeechGenerator
	{
		/// <summary>
		/// Generates speech from the given text.
		/// </summary>
		/// <param name="text">The text to generate speech for.</param>
		/// <returns>Generated audio samples.</returns>
		public float[] GenerateSpeech(string text);
	}
}