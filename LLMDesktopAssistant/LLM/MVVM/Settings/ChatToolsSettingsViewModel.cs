using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Tools;
using System.Collections.ObjectModel;
using System.Text;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// ViewModel for global chat tools settings (without agent-specific policy override).
/// Similar to <see cref="Agents.AgentToolSettingsViewModel"/> but without EnablePolicyOverride.
/// </summary>
[ViewModelFor(typeof(ChatToolsSettingsView))]
public class ChatToolsSettingsViewModel : ViewModelBase
{
	public ChatToolsSettings ToolSettings { get; }

	/// <summary>
	/// List of ToolBehaviour flags for Auto-Approve configuration.
	/// </summary>
	public ObservableCollection<ToolBehaviourItem> AutoApproveBehaviourItems { get; } = [];

	/// <summary>
	/// List of ToolBehaviour flags for Disallowed configuration.
	/// </summary>
	public ObservableCollection<ToolBehaviourItem> DisallowedBehaviourItems { get; } = [];

	public ChatToolsSettingsViewModel(ChatToolsSettings settings)
	{
		ToolSettings = settings;
		InitializeBehaviourItems();
	}

	private void InitializeBehaviourItems()
	{
		AutoApproveBehaviourItems.Clear();
		DisallowedBehaviourItems.Clear();

		foreach (var flag in GetBehaviourFlags())
		{
			var key = $"tool_behaviour_{flag.ToString().ToLower()}";
			var displayName = LocalizationManager.LocalizeStatic(key);
			var description = LocalizationManager.LocalizeStatic($"{key}_hint");

			// Fallback to CamelCase split if localization is missing
			if (displayName == key || string.IsNullOrEmpty(displayName))
				displayName = SplitCamelCase(flag.ToString());
			if (description == $"{key}_hint" || string.IsNullOrEmpty(description))
				description = string.Empty;

			AutoApproveBehaviourItems.Add(new ToolBehaviourItem(
				() => ToolSettings.AutoApproveBehaviours,
				v => ToolSettings.AutoApproveBehaviours = v,
				flag,
				displayName,
				description));

			DisallowedBehaviourItems.Add(new ToolBehaviourItem(
				() => ToolSettings.DisallowedBehaviours,
				v => ToolSettings.DisallowedBehaviours = v,
				flag,
				displayName,
				description));
		}
	}

	private static IEnumerable<ToolBehaviour> GetBehaviourFlags()
	{
		return Enum.GetValues<ToolBehaviour>()
			.Where(v => v != ToolBehaviour.None);
	}

	private static string SplitCamelCase(string input)
	{
		if (string.IsNullOrEmpty(input))
			return input;

		var result = new StringBuilder();
		for (int i = 0; i < input.Length; i++)
		{
			if (i > 0 && char.IsUpper(input[i]))
			{
				if (!char.IsUpper(input[i - 1]) || (i + 1 < input.Length && char.IsLower(input[i + 1])))
					result.Append(' ');
			}
			result.Append(input[i]);
		}
		return result.ToString();
	}
}
