using LLMDesktopAssistant.Modules;

namespace LLMDesktopAssistant.Speech
{
	/// <summary>
	/// Interface for user speech recognizer.
	/// </summary>
	public interface IUserSpeechRecognizer : IDynamicModule
	{
		/// <summary>
		/// Recognizes speech from audio samples.
		/// </summary>
		/// <param name="samples">Mono audio samples with 16 KHz sample rate to recognize.</param>
		/// <returns>Recognized text.</returns>
		public Task<string> RecognizeSpeechAsync(float[] samples);
	}
}