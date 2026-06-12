using Avalonia.Media;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Tools;
using Material.Icons;

namespace LLMDesktopAssistant.LLM.Messages;

/// <summary>
/// Represents a single ToolBehaviour flag for display in the UI.
/// Contains icon, color, and localized tooltip.
/// </summary>
public class ToolBehaviourFlagInfo
{
	/// <summary>
	/// The ToolBehaviour flag.
	/// </summary>
	public ToolBehaviour Flag { get; }

	/// <summary>
	/// Localized display name for the tooltip.
	/// </summary>
	public string DisplayName { get; }

	/// <summary>
	/// Localized description text for the tooltip.
	/// </summary>
	public string Description { get; }

	/// <summary>
	/// Icon to display for this flag.
	/// </summary>
	public MaterialIconKind Icon { get; }

	/// <summary>
	/// Color associated with this flag's severity/category.
	/// </summary>
	public IBrush Color { get; }

	public ToolBehaviourFlagInfo(ToolBehaviour flag, string displayName, string description, MaterialIconKind icon, IBrush color)
	{
		Flag = flag;
		DisplayName = displayName;
		Description = description;
		Icon = icon;
		Color = color;
	}

	/// <summary>
	/// Creates a <see cref="ToolBehaviourFlagInfo"/> for a given ToolBehaviour flag.
	/// </summary>
	public static ToolBehaviourFlagInfo Create(ToolBehaviour flag)
	{
		var key = $"tool_behaviour_{flag.ToString().ToLower()}";
		var displayName = LocalizationManager.LocalizeStatic(key);
		var description = LocalizationManager.LocalizeStatic($"{key}_hint");

		// Fallback to name if localization missing
		if (displayName == key || string.IsNullOrEmpty(displayName))
			displayName = flag.ToString();

		return new ToolBehaviourFlagInfo(flag, displayName, description, GetIcon(flag), GetColor(flag));
	}

	public static ImmutableList<ToolBehaviourFlagInfo> CreateForFlags(ToolBehaviour flags)
	{
		var result = ImmutableList.CreateBuilder<ToolBehaviourFlagInfo>();
		foreach (var flag in Enum.GetValues<ToolBehaviour>())
		{
			if (flag != ToolBehaviour.None && flags.HasFlag(flag))
			{
				result.Add(Create(flag));
			}
		}
		if (result.Count == 0 && flags == ToolBehaviour.None)
		{
			result.Add(Create(ToolBehaviour.None));
		}
		return result.ToImmutableList();
	}

	/// <summary>
	/// Determines the icon for a behaviour flag.
	/// </summary>
	private static MaterialIconKind GetIcon(ToolBehaviour flag) => flag switch
	{
		ToolBehaviour.None => MaterialIconKind.ShieldCheck,
		ToolBehaviour.FileDirectoryCreate => MaterialIconKind.FolderPlus,
		ToolBehaviour.FileRead => MaterialIconKind.FileDocument,
		ToolBehaviour.FileEdit => MaterialIconKind.FileEdit,
		ToolBehaviour.FileDelete => MaterialIconKind.FileRemove,
		ToolBehaviour.DirectoryRead => MaterialIconKind.FolderOpen,
		ToolBehaviour.DirectoryEdit => MaterialIconKind.FolderEdit,
		ToolBehaviour.DirectoryDelete => MaterialIconKind.FolderRemove,
		ToolBehaviour.ReadSecrets => MaterialIconKind.Key,
		ToolBehaviour.AccessOutsideWorkdir => MaterialIconKind.ExitRun,
		ToolBehaviour.ClipboardAccess => MaterialIconKind.Clipboard,
		ToolBehaviour.InternetAccess => MaterialIconKind.Web,
		ToolBehaviour.LongRunningTask => MaterialIconKind.TimerSand,
		ToolBehaviour.ExecuteExternalProcess => MaterialIconKind.Console,
		ToolBehaviour.PossiblyUnexpected => MaterialIconKind.AlertCircle,
		ToolBehaviour.RunTerminal => MaterialIconKind.Terminal,
		ToolBehaviour.UserInteraction => MaterialIconKind.Account,
		ToolBehaviour.AgentExecution => MaterialIconKind.Robot,
		ToolBehaviour.ToolAccess => MaterialIconKind.Tools,
		_ => MaterialIconKind.HelpCircle
	};

	/// <summary>
	/// Determines the color for a behaviour flag based on its severity.
	/// </summary>
	private static IBrush GetColor(ToolBehaviour flag) => flag switch
	{
		// Dangerous (red)
		ToolBehaviour.FileDelete => Brushes.Red,
		ToolBehaviour.DirectoryDelete => Brushes.Red,
		ToolBehaviour.ReadSecrets => Brushes.Red,
		ToolBehaviour.ExecuteExternalProcess => Brushes.Red,
		ToolBehaviour.PossiblyUnexpected => Brushes.Red,
		ToolBehaviour.ToolAccess => Brushes.Red,
		ToolBehaviour.AgentExecution => Brushes.Red,

		// Warning (yellow/orange)
		ToolBehaviour.FileEdit => Brushes.Orange,
		ToolBehaviour.DirectoryEdit => Brushes.Orange,
		ToolBehaviour.InternetAccess => Brushes.Orange,
		ToolBehaviour.RunTerminal => Brushes.Orange,
		ToolBehaviour.AccessOutsideWorkdir => Brushes.Orange,

		// Info (blue/cyan)
		ToolBehaviour.FileRead => Brushes.DodgerBlue,
		ToolBehaviour.DirectoryRead => Brushes.DodgerBlue,
		ToolBehaviour.FileDirectoryCreate => Brushes.DodgerBlue,
		ToolBehaviour.ClipboardAccess => Brushes.DodgerBlue,
		ToolBehaviour.LongRunningTask => Brushes.DodgerBlue,
		ToolBehaviour.UserInteraction => Brushes.DodgerBlue,

		// Safe (green)
		ToolBehaviour.None => Brushes.LimeGreen,

		_ => Brushes.Gray
	};
}
