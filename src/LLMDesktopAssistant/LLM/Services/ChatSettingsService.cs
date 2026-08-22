using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Settings;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// The default implementation of <see cref="IChatSettingsService"/>.
	/// Starts with the default <see cref="ChatSettings"/> profile loaded via <see cref="SettingsManager"/>.
	/// </summary>
	[ChatService(typeof(IChatSettingsService))]
	public class ChatSettingsService : IChatSettingsService
	{
		private ChatSettings? _settings;

		/// <inheritdoc/>
		public ChatSettings Settings => _settings ?? SettingsManager.Get<ChatSettings>();

		/// <inheritdoc/>
		public event EventHandler? SettingsChanged;

		/// <inheritdoc/>
		public void SetSettings(ChatSettings settings)
		{
			ArgumentNullException.ThrowIfNull(settings);
			if (ReferenceEquals(_settings, settings))
				return;

			_settings = settings;
			SettingsChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
