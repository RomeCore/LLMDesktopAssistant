using Avalonia.Media;
using LLMDesktopAssistant.Agents.SubAgents;
using LLMDesktopAssistant.Localization;
using Material.Icons;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// Represents a single <see cref="SubAgentDiagnosticCode"/> flag for display in the UI.
/// Contains icon, color, and localized tooltip.
/// </summary>
public class SubAgentDiagnosticFlagInfo
{
	/// <summary>
	/// The <see cref="SubAgentDiagnosticCode"/> flag.
	/// </summary>
	public SubAgentDiagnosticCode Flag { get; }

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

	public SubAgentDiagnosticFlagInfo(SubAgentDiagnosticCode flag, string displayName, string description,
		MaterialIconKind icon, IBrush color)
	{
		Flag = flag;
		DisplayName = displayName;
		Description = description;
		Icon = icon;
		Color = color;
	}

	/// <summary>
	/// Creates a <see cref="SubAgentDiagnosticFlagInfo"/> for a given <see cref="SubAgentDiagnosticCode"/> flag.
	/// </summary>
	public static SubAgentDiagnosticFlagInfo Create(SubAgentDiagnosticCode flag)
	{
		var key = $"subagent.diagnostic.{flag.ToString().ToLower()}";
		var displayName = LocalizationManager.LocalizeStatic(key);
		var description = LocalizationManager.LocalizeStatic($"{key}.hint");

		if (displayName == key || string.IsNullOrEmpty(displayName))
			displayName = flag.ToString();
		if (description == $"{key}.hint" || string.IsNullOrEmpty(description))
			description = string.Empty;

		return new SubAgentDiagnosticFlagInfo(flag, displayName, description, GetIcon(flag), GetColor(flag));
	}

	/// <summary>
	/// Creates a list of <see cref="SubAgentDiagnosticFlagInfo"/> for a set of diagnostic flags.
	/// </summary>
	public static ImmutableList<SubAgentDiagnosticFlagInfo> CreateForFlags(SubAgentDiagnosticCode codes)
	{
		var result = ImmutableList.CreateBuilder<SubAgentDiagnosticFlagInfo>();
		foreach (var flag in Enum.GetValues<SubAgentDiagnosticCode>())
		{
			if (flag is not SubAgentDiagnosticCode.None && codes.HasFlag(flag))
				result.Add(Create(flag));
		}
		return result.ToImmutableList();
	}

	/// <summary>
	/// Creates a list of <see cref="SubAgentDiagnosticFlagInfo"/> from a <see cref="SubAgentDiagnostic"/>.
	/// </summary>
	public static ImmutableList<SubAgentDiagnosticFlagInfo> CreateFromDiagnostic(SubAgentDiagnostic? diagnostic)
	{
		if (diagnostic == null || diagnostic.Codes == SubAgentDiagnosticCode.None)
			return [];

		var flags = CreateForFlags(diagnostic.Codes);

		// Add exception flag if present
		if (diagnostic.Exception != null)
		{
			var exceptionFlag = new SubAgentDiagnosticFlagInfo(
				SubAgentDiagnosticCode.GeneralParsingError,
				diagnostic.Exception.Message,
				diagnostic.Exception.StackTrace ?? string.Empty,
				MaterialIconKind.AlertCircle,
				diagnostic.IsFatal ? Brushes.Red : Brushes.Orange);
			flags = flags.Add(exceptionFlag);
		}

		return flags;
	}

	private static MaterialIconKind GetIcon(SubAgentDiagnosticCode flag) => flag switch
	{
		SubAgentDiagnosticCode.None => MaterialIconKind.CheckCircle,

		// Structure issues
		SubAgentDiagnosticCode.MissingYaml => MaterialIconKind.CodeJson,
		SubAgentDiagnosticCode.MissingName => MaterialIconKind.CardText,
		SubAgentDiagnosticCode.MissingDescription => MaterialIconKind.CardText,
		SubAgentDiagnosticCode.MissingYamlName => MaterialIconKind.CardText,
		SubAgentDiagnosticCode.MissingYamlDescription => MaterialIconKind.CardText,
		SubAgentDiagnosticCode.MissingFile => MaterialIconKind.FileQuestion,

		// Format issues
		SubAgentDiagnosticCode.YamlParsingError => MaterialIconKind.CodeBraces,
		SubAgentDiagnosticCode.YamlDecodingError => MaterialIconKind.CodeBraces,
		SubAgentDiagnosticCode.NameFormatError => MaterialIconKind.FormatLetterCase,
		SubAgentDiagnosticCode.NameFileMismatch => MaterialIconKind.FolderAlert,

		// Errors
		SubAgentDiagnosticCode.FileAccessError => MaterialIconKind.FileLock,
		SubAgentDiagnosticCode.GeneralParsingError => MaterialIconKind.AlertCircle,

		_ => MaterialIconKind.HelpCircle
	};

	private static IBrush GetColor(SubAgentDiagnosticCode flag) => flag switch
	{
		// Fatal-level issues (red)
		SubAgentDiagnosticCode.MissingFile => Brushes.Red,
		SubAgentDiagnosticCode.FileAccessError => Brushes.Red,
		SubAgentDiagnosticCode.GeneralParsingError => Brushes.Red,

		// Critical warnings (orange)
		SubAgentDiagnosticCode.MissingYaml => Brushes.Orange,
		SubAgentDiagnosticCode.YamlParsingError => Brushes.Orange,
		SubAgentDiagnosticCode.YamlDecodingError => Brushes.Orange,
		SubAgentDiagnosticCode.MissingName => Brushes.Orange,

		// Medium warnings (gold)
		SubAgentDiagnosticCode.NameFormatError => Brushes.Gold,
		SubAgentDiagnosticCode.NameFileMismatch => Brushes.Gold,
		SubAgentDiagnosticCode.MissingYamlName => Brushes.Gold,
		SubAgentDiagnosticCode.MissingDescription => Brushes.Gold,

		// Low warnings (dodger blue)
		SubAgentDiagnosticCode.MissingYamlDescription => Brushes.DodgerBlue,

		_ => Brushes.Gray
	};
}
