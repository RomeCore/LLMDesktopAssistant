using Avalonia.Media;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Prompting.Skills;
using Material.Icons;

namespace LLMDesktopAssistant.LLM.Messages;

/// <summary>
/// Represents a single <see cref="SkillDiagnosticCode"/> flag for display in the UI.
/// Contains icon, color, and localized tooltip.
/// </summary>
public class SkillDiagnosticFlagInfo
{
	/// <summary>
	/// The <see cref="SkillDiagnosticCode"/> flag.
	/// </summary>
	public SkillDiagnosticCode Flag { get; }

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

	public SkillDiagnosticFlagInfo(SkillDiagnosticCode flag, string displayName, string description,
		MaterialIconKind icon, IBrush color)
	{
		Flag = flag;
		DisplayName = displayName;
		Description = description;
		Icon = icon;
		Color = color;
	}

	/// <summary>
	/// Creates a <see cref="SkillDiagnosticFlagInfo"/> for a given <see cref="SkillDiagnosticCode"/> flag.
	/// </summary>
	public static SkillDiagnosticFlagInfo Create(SkillDiagnosticCode flag)
	{
		var key = $"skill_diagnostic_{flag.ToString().ToLower()}";
		var displayName = LocalizationManager.LocalizeStatic(key);
		var description = LocalizationManager.LocalizeStatic($"{key}_hint");

		if (displayName == key || string.IsNullOrEmpty(displayName))
			displayName = flag.ToString();
		if (description == $"{key}_hint" || string.IsNullOrEmpty(description))
			description = string.Empty;

		return new SkillDiagnosticFlagInfo(flag, displayName, description, GetIcon(flag), GetColor(flag));
	}

	/// <summary>
	/// Creates a list of <see cref="SkillDiagnosticFlagInfo"/> for a set of diagnostic flags.
	/// </summary>
	public static ImmutableList<SkillDiagnosticFlagInfo> CreateForFlags(SkillDiagnosticCode codes)
	{
		var result = ImmutableList.CreateBuilder<SkillDiagnosticFlagInfo>();
		foreach (var flag in Enum.GetValues<SkillDiagnosticCode>())
		{
			if (flag is not SkillDiagnosticCode.None && codes.HasFlag(flag))
				result.Add(Create(flag));
		}
		return result.ToImmutableList();
	}

	/// <summary>
	/// Creates a list of <see cref="SkillDiagnosticFlagInfo"/> from a <see cref="SkillDiagnostic"/>.
	/// </summary>
	public static ImmutableList<SkillDiagnosticFlagInfo> CreateFromDiagnostic(SkillDiagnostic? diagnostic)
	{
		if (diagnostic == null || diagnostic.Codes == SkillDiagnosticCode.None)
			return [];

		var flags = CreateForFlags(diagnostic.Codes);

		// Add exception flag if present
		if (diagnostic.Exception != null)
		{
			var exceptionFlag = new SkillDiagnosticFlagInfo(
				SkillDiagnosticCode.GeneralParsingError,
				diagnostic.Exception.Message,
				diagnostic.Exception.StackTrace ?? string.Empty,
				MaterialIconKind.AlertCircle,
				diagnostic.IsFatal ? Brushes.Red : Brushes.Orange);
			flags = flags.Add(exceptionFlag);
		}

		return flags;
	}

	private static MaterialIconKind GetIcon(SkillDiagnosticCode flag) => flag switch
	{
		SkillDiagnosticCode.None => MaterialIconKind.CheckCircle,

		// Structure issues
		SkillDiagnosticCode.MissingYaml => MaterialIconKind.CodeJson,
		SkillDiagnosticCode.MissingName => MaterialIconKind.CardText,
		SkillDiagnosticCode.MissingDescription => MaterialIconKind.CardText,
		SkillDiagnosticCode.MissingYamlName => MaterialIconKind.CardText,
		SkillDiagnosticCode.MissingYamlDescription => MaterialIconKind.CardText,
		SkillDiagnosticCode.MissingFile => MaterialIconKind.FileQuestion,

		// Format issues
		SkillDiagnosticCode.YamlParsingError => MaterialIconKind.CodeBraces,
		SkillDiagnosticCode.YamlDecodingError => MaterialIconKind.CodeBraces,
		SkillDiagnosticCode.NameFormatError => MaterialIconKind.FormatLetterCase,
		SkillDiagnosticCode.NameDirectoryMismatch => MaterialIconKind.FolderAlert,

		// Errors
		SkillDiagnosticCode.FileAccessError => MaterialIconKind.FileLock,
		SkillDiagnosticCode.GeneralParsingError => MaterialIconKind.AlertCircle,

		_ => MaterialIconKind.HelpCircle
	};

	private static IBrush GetColor(SkillDiagnosticCode flag) => flag switch
	{
		// Fatal-level issues (red)
		SkillDiagnosticCode.MissingFile => Brushes.Red,
		SkillDiagnosticCode.FileAccessError => Brushes.Red,
		SkillDiagnosticCode.GeneralParsingError => Brushes.Red,

		// Critical warnings (orange)
		SkillDiagnosticCode.MissingYaml => Brushes.Orange,
		SkillDiagnosticCode.YamlParsingError => Brushes.Orange,
		SkillDiagnosticCode.YamlDecodingError => Brushes.Orange,
		SkillDiagnosticCode.MissingName => Brushes.Orange,

		// Medium warnings (gold)
		SkillDiagnosticCode.NameFormatError => Brushes.Gold,
		SkillDiagnosticCode.NameDirectoryMismatch => Brushes.Gold,
		SkillDiagnosticCode.MissingYamlName => Brushes.Gold,
		SkillDiagnosticCode.MissingDescription => Brushes.Gold,

		// Low warnings (dodger blue)
		SkillDiagnosticCode.MissingYamlDescription => Brushes.DodgerBlue,

		_ => Brushes.Gray
	};
}
