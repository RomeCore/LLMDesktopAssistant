using LLMDesktopAssistant.Services;

namespace LLMDesktopAssistant.Speech
{
	/// <summary>
	/// Interface for speech player from text.
	/// </summary>
	public interface IAssistantSpeechPlayer
	{
		/// <summary>
		/// Plays the provided text as speech.
		/// </summary>
		/// <param name="text">The text to play as speech.</param>
		/// <returns>A task that completes when the speech has finished playing.</returns>
		public Task SpeakAsync(string text, CancellationToken cancellationToken = default);
	}
}