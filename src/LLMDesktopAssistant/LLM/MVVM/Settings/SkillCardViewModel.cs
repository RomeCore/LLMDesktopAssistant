using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.LLM.Messages;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Prompting.Skills;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;
using Material.Icons;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// A metadata entry of a skill with a localized key display name.
/// </summary>
public class SkillMetadataItem
{
	/// <summary>
	/// Gets the display name of the metadata key.
	/// </summary>
	public string Key { get; }

	/// <summary>
	/// Gets the metadata value.
	/// </summary>
	public string Value { get; }

	public SkillMetadataItem(string key, string value)
	{
		Key = key;
		Value = value;
	}
}

/// <summary>
/// Represents a <see cref="SkillInjectionMode"/> value with a localized display name for use in ComboBox.
/// </summary>
public class SkillInjectionModeItem
{
	/// <summary>
	/// Gets the <see cref="SkillInjectionMode"/> value.
	/// </summary>
	public SkillInjectionMode Value { get; }

	/// <summary>
	/// Gets the localized display name.
	/// </summary>
	public string DisplayName { get; }

	public SkillInjectionModeItem(SkillInjectionMode value)
	{
		Value = value;
		var key = $"skill.injection_mode.{value.ToString().ToLower()}";
		DisplayName = LocalizationManager.LocalizeStatic(key);

		// Fallback to enum name if localization missing
		if (DisplayName == key || string.IsNullOrEmpty(DisplayName))
			DisplayName = value.ToString();
	}

	/// <summary>
	/// Gets all <see cref="SkillInjectionMode"/> values with localized display names.
	/// </summary>
	public static ImmutableList<SkillInjectionModeItem> All { get; } =
		Enum.GetValues<SkillInjectionMode>()
			.Select(v => new SkillInjectionModeItem(v))
			.ToImmutableList();
}

/// <summary>
/// ViewModel for a single skill card in the settings UI. The card is reused by the
/// chat-level skill list and the per-agent override list. When <see cref="CanToggle"/>
/// is <see langword="true"/>, the card edits a <see cref="SkillChange"/> (enabled + injection mode);
/// otherwise it displays the skill definition read-only.
/// </summary>
public class SkillCardViewModel : ViewModelBase
{
	private readonly SkillInfo _info;
	private readonly RangeObservableCollection<SkillChange>? _changes;
	private readonly Action<string>? _onTagClick;
	private readonly Action? _onDeleted;
	private readonly IExplorerOpener? _explorerOpener;
	private SkillChange? _change;
	private bool _isDetailsVisible;
	private string? _body;

	/// <summary>
	/// Initializes a new instance of the <see cref="SkillCardViewModel"/> class.
	/// </summary>
	/// <param name="info">The skill information to display.</param>
	/// <param name="canToggle">Whether the card edits a <see cref="SkillChange"/> (agent settings) or is read-only (chat settings).</param>
	/// <param name="change">The existing change for this skill, if any.</param>
	/// <param name="changes">The effective change collection to modify, required when <paramref name="canToggle"/> is <see langword="true"/>.</param>
	/// <param name="onTagClick">A callback invoked when a tag chip is clicked.</param>
	/// <param name="onDeleted">A callback invoked after the skill file was deleted.</param>
	public SkillCardViewModel(SkillInfo info, bool canToggle,
		SkillChange? change = null,
		RangeObservableCollection<SkillChange>? changes = null,
		Action<string>? onTagClick = null,
		Action? onDeleted = null)
	{
		_info = info;
		CanToggle = canToggle;
		_change = change;
		_changes = changes;
		_onTagClick = onTagClick;
		_onDeleted = onDeleted;
		_explorerOpener = ServiceRegistry.Provider.GetService<IExplorerOpener>();

		DiagnosticFlags = SkillDiagnosticFlagInfo.CreateFromDiagnostic(info.Diagnostic);

		AllowedTools = info.AllowedTools.Select(FormatTool).ToImmutableList();
		AvailableTools = info.AvailableTools.Select(FormatTool).ToImmutableList();
		DisallowedTools = info.DisallowedTools.Select(FormatTool).ToImmutableList();
		MetadataItems = info.Metadata
			.Select(kvp => new SkillMetadataItem(LocalizeMetadataKey(kvp.Key), kvp.Value))
			.Concat(info.AdditionalMetadata.Select(kvp => new SkillMetadataItem(kvp.Key, kvp.Value)))
			.ToImmutableList();

		ToggleDetailsCommand = new RelayCommand(() => IsDetailsVisible = !IsDetailsVisible);
		ResetCommand = new RelayCommand(Reset, () => CanToggle);
		FilterByTagCommand = new RelayCommand<string?>(tag =>
		{
			if (!string.IsNullOrEmpty(tag))
				_onTagClick?.Invoke(tag);
		});
		ShowInExplorerCommand = new RelayCommand(ShowInExplorer, () => HasPath);
		OpenFileCommand = new RelayCommand(OpenFile, () => HasPath);
		DeleteFileCommand = new AsyncRelayCommand(DeleteFileAsync, () => HasPath);
	}

	/// <summary>
	/// Gets the underlying <see cref="SkillInfo"/>.
	/// </summary>
	public SkillInfo Info => _info;

	/// <summary>
	/// Gets a value indicating whether the card edits a <see cref="SkillChange"/>.
	/// </summary>
	public bool CanToggle { get; }

	/// <summary>
	/// Gets the name of the skill.
	/// </summary>
	public string Name => _info.Name;

	/// <summary>
	/// Gets the description of the skill.
	/// </summary>
	public string Description => _info.Description;

	/// <summary>
	/// Gets the file path of the skill, if applicable.
	/// </summary>
	public string? Path => _info.Path;

	/// <summary>
	/// Gets a value indicating whether the skill file exists on disk.
	/// </summary>
	public bool HasPath => !string.IsNullOrEmpty(_info.Path) && File.Exists(_info.Path);

	/// <summary>
	/// Gets a value indicating whether the skill comes from a file that can be edited or deleted.
	/// </summary>
	public bool IsFileBased => _info.Source is SkillSource.UserProfile or SkillSource.WorkingDirectory or SkillSource.Custom;

	/// <summary>
	/// Gets the source of the skill.
	/// </summary>
	public SkillSource Source => _info.Source;

	/// <summary>
	/// Gets the localized display name of the source.
	/// </summary>
	public string SourceDisplayName => LocalizeSource(_info.Source);

	/// <summary>
	/// Gets the icon of the source.
	/// </summary>
	public MaterialIconKind SourceIcon => _info.Source switch
	{
		SkillSource.UserProfile => MaterialIconKind.AccountCircle,
		SkillSource.WorkingDirectory => MaterialIconKind.Folder,
		SkillSource.Custom => MaterialIconKind.FolderStar,
		SkillSource.Template => MaterialIconKind.FileCode,
		_ => MaterialIconKind.HelpCircle
	};

	/// <summary>
	/// Gets the localized display name of the injection mode defined in the skill.
	/// </summary>
	public string DefinitionInjectionMode => LocalizeInjectionMode(_info.InjectionMode);

	/// <summary>
	/// Gets or sets whether the skill is enabled. Returns the skill-defined value when
	/// <see cref="CanToggle"/> is <see langword="false"/>.
	/// </summary>
	public bool Enabled
	{
		get => _change?.Enabled ?? _info.Enabled;
		set
		{
			if (Enabled != value)
			{
				EnsureChange().Enabled = value;
				RaisePropertyChanged(nameof(Enabled));
			}
		}
	}

	/// <summary>
	/// Gets the list of diagnostic flag infos for display in the UI.
	/// </summary>
	public ImmutableList<SkillDiagnosticFlagInfo> DiagnosticFlags { get; }

	/// <summary>
	/// Gets the tags of the skill.
	/// </summary>
	public ImmutableList<string> Tags => _info.Tags;

	/// <summary>
	/// Gets the list of available injection modes for the ComboBox.
	/// </summary>
	public ImmutableList<SkillInjectionModeItem> InjectionModeList { get; } = SkillInjectionModeItem.All;

	/// <summary>
	/// Gets or sets the selected injection mode. Setting a non-null value creates a
	/// <see cref="SkillChange"/> with the injection mode override.
	/// </summary>
	public SkillInjectionModeItem? SelectedInjectionMode
	{
		get => InjectionModeList.FirstOrDefault(i => i.Value == (_change?.InjectionMode ?? _info.InjectionMode));
		set
		{
			if (SelectedInjectionMode != value && value != null)
			{
				EnsureChange().InjectionMode = value.Value;
				RaisePropertyChanged(nameof(SelectedInjectionMode));
			}
		}
	}

	/// <summary>
	/// Gets the number of tools referenced by the skill.
	/// </summary>
	public int ToolCount => AllowedTools.Count + AvailableTools.Count + DisallowedTools.Count;

	/// <summary>
	/// Gets a value indicating whether any badge should be displayed.
	/// </summary>
	public bool HasBadges => ToolCount > 0;

	/// <summary>
	/// Gets the localized tool count badge text, or <see langword="null"/> when zero.
	/// </summary>
	public string? ToolsBadge => FormatBadge("settings.skills.badge.tools", ToolCount);

	/// <summary>
	/// Gets the tools used without approval.
	/// </summary>
	public ImmutableList<string> AllowedTools { get; }

	/// <summary>
	/// Gets the tools available when the skill activates.
	/// </summary>
	public ImmutableList<string> AvailableTools { get; }

	/// <summary>
	/// Gets the tools disallowed when the skill activates.
	/// </summary>
	public ImmutableList<string> DisallowedTools { get; }

	/// <summary>
	/// Gets the metadata entries of the skill.
	/// </summary>
	public ImmutableList<SkillMetadataItem> MetadataItems { get; }

	/// <summary>
	/// Gets the body (SKILL.md content excluding the YAML frontmatter) of the skill.
	/// </summary>
	public string Body => _body ??= _info.BodyGetter();

	/// <summary>
	/// Gets a value indicating whether the details section has any content.
	/// </summary>
	public bool HasDetails => !string.IsNullOrWhiteSpace(Body)
		|| AllowedTools.Count > 0 || AvailableTools.Count > 0 || DisallowedTools.Count > 0
		|| MetadataItems.Count > 0;

	/// <summary>
	/// Gets or sets a value indicating whether the details section is visible.
	/// </summary>
	public bool IsDetailsVisible
	{
		get => _isDetailsVisible;
		set => SetProperty(ref _isDetailsVisible, value);
	}

	/// <summary>
	/// Gets the command that toggles the details section visibility.
	/// </summary>
	public ICommand ToggleDetailsCommand { get; }

	/// <summary>
	/// Gets the command that resets the per-agent changes of this skill.
	/// </summary>
	public ICommand ResetCommand { get; }

	/// <summary>
	/// Gets the command that applies a tag filter by clicking a tag chip.
	/// </summary>
	public ICommand FilterByTagCommand { get; }

	/// <summary>
	/// Gets the command that reveals the skill file in the system file explorer.
	/// </summary>
	public ICommand ShowInExplorerCommand { get; }

	/// <summary>
	/// Gets the command that opens the skill file with the default application.
	/// </summary>
	public ICommand OpenFileCommand { get; }

	/// <summary>
	/// Gets the command that deletes the skill file after confirmation.
	/// </summary>
	public ICommand DeleteFileCommand { get; }

	private void Reset()
	{
		if (_change != null && _changes != null)
		{
			_changes.Remove(_change);
			_change = null;
			RaisePropertyChanged(nameof(Enabled));
			RaisePropertyChanged(nameof(SelectedInjectionMode));
		}
	}

	private SkillChange EnsureChange()
	{
		if (_change == null)
		{
			_change = new SkillChange
			{
				SkillName = Name,
				Enabled = null,
				InjectionMode = null
			};
			_changes!.Add(_change);
		}
		return _change;
	}

	private void ShowInExplorer()
	{
		if (_info.Path != null)
			_explorerOpener?.ShowFileInExplorer(_info.Path);
	}

	private void OpenFile()
	{
		if (_info.Path == null)
			return;

		try
		{
			Process.Start(new ProcessStartInfo(_info.Path) { UseShellExecute = true });
		}
		catch (Exception ex)
		{
			ServiceRegistry.Provider.GetRequiredService<IToastService>()
				.ShowError(LocalizationManager.LocalizeStatic("common.error"), ex.Message);
		}
	}

	private async Task DeleteFileAsync()
	{
		if (_info.Path == null || !HasPath)
			return;

		var confirm = new ConfirmDialogViewModel
		{
			Title = LocalizationManager.LocalizeStatic("settings.skills.delete.title"),
			Description = LocalizationManager.LocalizeStaticFormat("settings.skills.delete.confirm", _info.Path),
			ConfirmText = LocalizationManager.LocalizeStatic("common.delete"),
			CancelText = LocalizationManager.LocalizeStatic("common.cancel"),
			IsDanger = true
		};

#pragma warning disable CS8605 // Unboxing is safe: the confirmation dialog always returns bool.
		var confirmed = (bool)await DialogManager.ShowDialogAsync(confirm);
#pragma warning restore CS8605
		if (!confirmed)
			return;

		try
		{
			File.Delete(_info.Path);

			// Remove the (now empty) skill home directory if it contains only the deleted file.
			if (_info.HomeDirectory != null && Directory.Exists(_info.HomeDirectory)
				&& !Directory.EnumerateFileSystemEntries(_info.HomeDirectory).Any())
			{
				Directory.Delete(_info.HomeDirectory);
			}

			_onDeleted?.Invoke();
		}
		catch (Exception ex)
		{
			ServiceRegistry.Provider.GetRequiredService<IToastService>()
				.ShowError(LocalizationManager.LocalizeStatic("common.error"), ex.Message);
		}
	}

	private static string FormatTool(ToolNameWithSpecifier tool) =>
		tool.Specifier == null ? tool.ToolName : $"{tool.ToolName}({tool.Specifier})";

	private static string LocalizeMetadataKey(SkillMetadataType type)
	{
		var key = $"skill.metadata.{type.ToString().ToLower()}";
		var localized = LocalizationManager.LocalizeStatic(key);
		return localized == key ? type.ToString() : localized;
	}

	private static string LocalizeInjectionMode(SkillInjectionMode mode)
	{
		var key = $"skill.injection_mode.{mode.ToString().ToLower()}";
		var localized = LocalizationManager.LocalizeStatic(key);
		return localized == key ? mode.ToString() : localized;
	}

	private static string LocalizeSource(SkillSource source)
	{
		var key = $"skill.source.{source.ToString().ToLower()}";
		var localized = LocalizationManager.LocalizeStatic(key);
		return localized == key ? source.ToString() : localized;
	}

	private static string? FormatBadge(string key, int count) =>
		count > 0 ? LocalizationManager.LocalizeStaticFormat(key, count) : null;
}
