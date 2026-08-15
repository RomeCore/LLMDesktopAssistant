using LLMDesktopAssistant.LLM.Settings;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// ViewModel for global chat tools settings (without agent-specific policy).
/// The agent tool policy is configured in <see cref="Agents.AgentToolSettingsViewModel"/>.
/// </summary>
[ViewModelFor(typeof(ChatToolsSettingsView))]
public class ChatToolsSettingsViewModel : ViewModelBase
{
	/// <summary>
	/// Gets the underlying chat tool settings.
	/// </summary>
	public ChatToolSettings ToolSettings { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatToolsSettingsViewModel"/> class.
	/// </summary>
	/// <param name="settings">The chat tool settings to edit.</param>
	public ChatToolsSettingsViewModel(ChatToolSettings settings)
	{
		ToolSettings = settings;
	}
}
