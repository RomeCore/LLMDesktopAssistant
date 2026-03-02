using LLMDesktopAssistant.Modules;

namespace LLMDesktopAssistant.Speech
{
	/// <summary>
	/// Interface for playing generated speech.
	/// </summary>
	public interface ISpeechPlayer : IDynamicModule
	{
		/// <summary>
		/// Plays the given audio samples.
		/// </summary>
		/// <param name="samples">The audio samples to play.</param>
		public void Play(float[] samples);
	}
}