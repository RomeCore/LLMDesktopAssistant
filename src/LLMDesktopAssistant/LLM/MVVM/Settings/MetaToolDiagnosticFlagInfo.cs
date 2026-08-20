using Avalonia.Media;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Tools.Meta;
using Material.Icons;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// Represents a single <see cref="MetaToolDiagnosticCode"/> flag for display in the UI.
/// Contains icon, color, and localized tooltip.
/// </summary>
public class MetaToolDiagnosticFlagInfo
{
	/// <summary>
	/// Gets the diagnostic code flag.
	/// </summary>
	public MetaToolDiagnosticCode Flag { get; }

	/// <summary>
	/// Gets the localized display name for the tooltip.
	/// </summary>
	public string DisplayName { get; }

	/// <summary>
	/// Gets the icon to display for this flag.
	/// </summary>
	public MaterialIconKind Icon { get; }

	/// <summary>
	/// Gets the color associated with this flag's severity.
	/// </summary>
	public IBrush Color { get; }

	public MetaToolDiagnosticFlagInfo(MetaToolDiagnosticCode flag, string displayName, MaterialIconKind icon, IBrush color)
	{
		Flag = flag;
		DisplayName = displayName;
		Icon = icon;
		Color = color;
	}

	/// <summary>
	/// Creates a <see cref="MetaToolDiagnosticFlagInfo"/> for a given diagnostic code flag.
	/// </summary>
	public static MetaToolDiagnosticFlagInfo Create(MetaToolDiagnosticCode flag)
	{
		var key = $"settings.tools.meta_tools.diagnostic.{flag.ToString().ToLower()}";
		var displayName = LocalizationManager.LocalizeStatic(key);

		if (displayName == key || string.IsNullOrEmpty(displayName))
			displayName = flag.ToString();

		return new MetaToolDiagnosticFlagInfo(flag, displayName, GetIcon(flag), GetColor(flag));
	}

	/// <summary>
	/// Creates the display infos for all flags set in the given diagnostic codes.
	/// </summary>
	public static ImmutableList<MetaToolDiagnosticFlagInfo> CreateForCodes(MetaToolDiagnosticCode codes)
	{
		var result = ImmutableList.CreateBuilder<MetaToolDiagnosticFlagInfo>();
		foreach (var flag in Enum.GetValues<MetaToolDiagnosticCode>())
		{
			if (flag is not MetaToolDiagnosticCode.None && codes.HasFlag(flag))
				result.Add(Create(flag));
		}
		return result.ToImmutable();
	}

	private static MaterialIconKind GetIcon(MetaToolDiagnosticCode flag) => flag switch
	{
		MetaToolDiagnosticCode.MissingFrontmatter => MaterialIconKind.CodeBraces,
		MetaToolDiagnosticCode.FrontmatterParsingError => MaterialIconKind.CodeBraces,
		MetaToolDiagnosticCode.FrontmatterDecodingError => MaterialIconKind.CodeBraces,
		MetaToolDiagnosticCode.NameFormatError => MaterialIconKind.Alphabetical,
		MetaToolDiagnosticCode.InvalidApprovalLevel => MaterialIconKind.ShieldAlert,
		MetaToolDiagnosticCode.InvalidBehaviours => MaterialIconKind.ShieldAlert,
		MetaToolDiagnosticCode.InvalidArgumentSchema => MaterialIconKind.CodeJson,
		MetaToolDiagnosticCode.MissingFile => MaterialIconKind.FileQuestion,
		MetaToolDiagnosticCode.FileAccessError => MaterialIconKind.FileLock,
		MetaToolDiagnosticCode.GeneralParsingError => MaterialIconKind.AlertCircle,
		_ => MaterialIconKind.HelpCircle
	};

	private static IBrush GetColor(MetaToolDiagnosticCode flag) => flag switch
	{
		MetaToolDiagnosticCode.MissingFrontmatter => Brushes.Orange,
		MetaToolDiagnosticCode.FrontmatterParsingError => Brushes.Red,
		MetaToolDiagnosticCode.FrontmatterDecodingError => Brushes.Red,
		MetaToolDiagnosticCode.NameFormatError => Brushes.Orange,
		MetaToolDiagnosticCode.InvalidApprovalLevel => Brushes.Orange,
		MetaToolDiagnosticCode.InvalidBehaviours => Brushes.Orange,
		MetaToolDiagnosticCode.InvalidArgumentSchema => Brushes.Red,
		MetaToolDiagnosticCode.MissingFile => Brushes.Red,
		MetaToolDiagnosticCode.FileAccessError => Brushes.Red,
		MetaToolDiagnosticCode.GeneralParsingError => Brushes.Red,
		_ => Brushes.Gray
	};
}
