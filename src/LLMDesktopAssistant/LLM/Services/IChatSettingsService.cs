using LLMDesktopAssistant.LLM.Settings;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// Provides access to the settings of the current chat session.
	/// The service owns the current <see cref="ChatSettings"/> instance and notifies
	/// subscribers when it is replaced (e.g. when a different settings profile is loaded).
	/// </summary>
	public interface IChatSettingsService
	{
		/// <summary>
		/// Gets the current chat settings.
		/// </summary>
		ChatSettings Settings { get; }

		/// <summary>
		/// Occurs when the current settings instance is replaced.
		/// </summary>
		event EventHandler? SettingsChanged;

		/// <summary>
		/// Replaces the current settings instance with the given one.
		/// </summary>
		/// <param name="settings">The new settings instance.</param>
		void SetSettings(ChatSettings settings);
	}
}
