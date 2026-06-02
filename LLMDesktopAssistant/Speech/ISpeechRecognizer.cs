using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Speech
{
	/// <summary>
	/// Interface for speech recognizer.
	/// </summary>
	public interface ISpeechRecognizer
	{
		/// <summary>
		/// Recognizes speech from audio samples.
		/// </summary>
		/// <param name="samples">Mono audio samples with 16 KHz sample rate to recognize.</param>
		/// <returns>Recognized text.</returns>
		public Task<string> RecognizeSpeechAsync(float[] samples);
	}
}