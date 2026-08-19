using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.SubAgents;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.LLM.Messages;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;
using Material.Icons;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// A memory block reference of a sub-agent with a localized attachment mode display name.
/// </summary>
public class SubAgentMemoryBlockItem
{
	/// <summary>
	/// Gets the name of the memory block.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the localized display name of the attachment mode.
	/// </summary>
	public string ModeDisplayName { get; }

	public SubAgentMemoryBlockItem(string name, MemoryBlockAttachmentMode mode)
	{
		Name = name;
		var key = $"memory.attachment_mode.{mode.ToString().ToLower()}";
		var localized = LocalizationManager.LocalizeStatic(key);
		ModeDisplayName = localized == key ? mode.ToString() : localized;
	}
}

/// <summary>
/// A metadata entry of a sub-agent with a localized key display name.
/// </summary>
public class SubAgentMetadataItem
{
	/// <summary>
	/// Gets the display name of the metadata key.
	/// </summary>
	public string Key { get; }

	/// <summary>
	/// Gets the metadata value.
	/// </summary>
	public string Value { get; }

	public SubAgentMetadataItem(string key, string value)
	{
		Key = key;
		Value = value;
	}
}

/// <summary>
/// A broken dependency issue of a sub-agent with a localized display text.
/// </summary>
public class SubAgentLinkIssueItem
{
	/// <summary>
	/// Gets the localized display text of the issue.
	/// </summary>
	public string Text { get; }

	public SubAgentLinkIssueItem(SubAgentLinkIssue issue)
	{
		Text = SubAgentCardViewModel.GetLinkIssueText(issue);
	}
}


/// <summary>
/// ViewModel for a single sub-agent card in the settings UI. The card is reused by the
/// chat-level sub-agent list and the per-agent override list. When <see cref="CanToggle"/>
/// is <see langword="true"/>, the card edits a <see cref="SubAgentChange"/> (enabled + model);
/// otherwise it displays the sub-agent definition read-only.
/// </summary>
public class SubAgentCardViewModel : ViewModelBase
{
	private readonly SubAgentInfo _info;
	private readonly RangeObservableCollection<SubAgentChange>? _changes;
	private readonly Action<string>? _onTagClick;
	private readonly Action? _onDeleted;
	private readonly IExplorerOpener? _explorerOpener;
	private SubAgentChange? _change;
	private bool _isDetailsVisible;
	private string? _systemPrompt;

	/// <summary>
	/// Initializes a new instance of the <see cref="SubAgentCardViewModel"/> class.
	/// </summary>
	/// <param name="info">The sub-agent information to display.</param>
	/// <param name="canToggle">Whether the card edits a <see cref="SubAgentChange"/> (agent settings) or is read-only (chat settings).</param>
	/// <param name="change">The existing change for this sub-agent, if any.</param>
	/// <param name="changes">The effective change collection to modify, required when <paramref name="canToggle"/> is <see langword="true"/>.</param>
	/// <param name="linkIssues">The broken dependency issues found for this sub-agent.</param>
	/// <param name="onTagClick">A callback invoked when a tag chip is clicked.</param>
	/// <param name="onDeleted">A callback invoked after the sub-agent file was deleted.</param>
	public SubAgentCardViewModel(SubAgentInfo info, bool canToggle,
		SubAgentChange? change = null,
		RangeObservableCollection<SubAgentChange>? changes = null,
		IEnumerable<SubAgentLinkIssue>? linkIssues = null,
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

		DiagnosticFlags = SubAgentDiagnosticFlagInfo.CreateFromDiagnostic(info.Diagnostic);
		LinkIssues = linkIssues?.ToImmutableList() ?? [];
		LinkIssueItems = LinkIssues.Select(i => new SubAgentLinkIssueItem(i)).ToImmutableList();

		AllowedTools = info.AllowedTools.Select(FormatTool).ToImmutableList();
		AvailableTools = info.AvailableTools.Select(FormatTool).ToImmutableList();
		DisallowedTools = info.DisallowedTools.Select(FormatTool).ToImmutableList();
		Skills = info.Skills;
		SubAgentNames = info.SubAgents;
		MemoryBlocks = info.MemoryBlocks
			.Select(kvp => new SubAgentMemoryBlockItem(kvp.Key, kvp.Value))
			.ToImmutableList();
		MetadataItems = info.Metadata
			.Select(kvp => new SubAgentMetadataItem(LocalizeMetadataKey(kvp.Key), kvp.Value))
			.Concat(info.AdditionalMetadata.Select(kvp => new SubAgentMetadataItem(kvp.Key, kvp.Value)))
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
	/// Gets the underlying <see cref="SubAgentInfo"/>.
	/// </summary>
	public SubAgentInfo Info => _info;

	/// <summary>
	/// Gets a value indicating whether the card edits a <see cref="SubAgentChange"/>.
	/// </summary>
	public bool CanToggle { get; }

	/// <summary>
	/// Gets the name of the sub-agent.
	/// </summary>
	public string Name => _info.Name;

	/// <summary>
	/// Gets the description of the sub-agent.
	/// </summary>
	public string Description => _info.Description;

	/// <summary>
	/// Gets the file path of the sub-agent, if applicable.
	/// </summary>
	public string? Path => _info.Path;

	/// <summary>
	/// Gets a value indicating whether the sub-agent file exists on disk.
	/// </summary>
	public bool HasPath => !string.IsNullOrEmpty(_info.Path) && File.Exists(_info.Path);

	/// <summary>
	/// Gets a value indicating whether the sub-agent comes from a file that can be edited or deleted.
	/// </summary>
	public bool IsFileBased => _info.Source is SubAgentSource.UserProfile or SubAgentSource.WorkingDirectory or SubAgentSource.Custom;

	/// <summary>
	/// Gets the source of the sub-agent.
	/// </summary>
	public SubAgentSource Source => _info.Source;

	/// <summary>
	/// Gets the localized display name of the source.
	/// </summary>
	public string SourceDisplayName => LocalizeSource(_info.Source);

	/// <summary>
	/// Gets the icon of the source.
	/// </summary>
	public MaterialIconKind SourceIcon => _info.Source switch
	{
		SubAgentSource.UserProfile => MaterialIconKind.AccountCircle,
		SubAgentSource.WorkingDirectory => MaterialIconKind.Folder,
		SubAgentSource.Custom => MaterialIconKind.FolderStar,
		SubAgentSource.Template => MaterialIconKind.FileCode,
		_ => MaterialIconKind.HelpCircle
	};

	/// <summary>
	/// Gets the model defined in the sub-agent, if any.
	/// </summary>
	public string? DefinitionModel => _info.Model;

	/// <summary>
	/// Gets or sets whether the sub-agent is enabled. Returns <see langword="null"/> for inherited
	/// (default) value when <see cref="CanToggle"/> is <see langword="true"/>.
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
	public ImmutableList<SubAgentDiagnosticFlagInfo> DiagnosticFlags { get; }

	/// <summary>
	/// Gets the list of broken dependency issues found for this sub-agent.
	/// </summary>
	public ImmutableList<SubAgentLinkIssue> LinkIssues { get; }

	/// <summary>
	/// Gets the list of broken dependency issues with localized display text.
	/// </summary>
	public ImmutableList<SubAgentLinkIssueItem> LinkIssueItems { get; }

	/// <summary>
	/// Gets the tags of the sub-agent.
	/// </summary>
	public ImmutableList<string> Tags => _info.Tags;

	/// <summary>
	/// Gets the localized text of a link issue.
	/// </summary>
	public static string GetLinkIssueText(SubAgentLinkIssue issue)
	{
		var key = issue.Kind switch
		{
			SubAgentLinkIssueKind.Skill => "settings.sub_agents.links.skill_not_found",
			SubAgentLinkIssueKind.SubAgent => "settings.sub_agents.links.sub_agent_not_found",
			_ => "settings.sub_agents.links.memory_block_not_found"
		};
		return LocalizationManager.LocalizeStaticFormat(key, issue.Name);
	}

	/// <summary>
	/// Gets the number of tools referenced by the sub-agent.
	/// </summary>
	public int ToolCount => AllowedTools.Count + AvailableTools.Count + DisallowedTools.Count;

	/// <summary>
	/// Gets the number of skills referenced by the sub-agent.
	/// </summary>
	public int SkillCount => Skills.Count;

	/// <summary>
	/// Gets the number of nested sub-agents referenced by the sub-agent.
	/// </summary>
	public int SubAgentCount => SubAgentNames.Count;

	/// <summary>
	/// Gets the number of memory blocks referenced by the sub-agent.
	/// </summary>
	public int MemoryBlockCount => MemoryBlocks.Count;

	/// <summary>
	/// Gets a value indicating whether any badge should be displayed.
	/// </summary>
	public bool HasBadges => ToolCount > 0 || SkillCount > 0 || SubAgentCount > 0 || MemoryBlockCount > 0;

	/// <summary>
	/// Gets the localized tool count badge text, or <see langword="null"/> when zero.
	/// </summary>
	public string? ToolsBadge => FormatBadge("settings.sub_agents.badge.tools", ToolCount);

	/// <summary>
	/// Gets the localized skill count badge text, or <see langword="null"/> when zero.
	/// </summary>
	public string? SkillsBadge => FormatBadge("settings.sub_agents.badge.skills", SkillCount);

	/// <summary>
	/// Gets the localized nested sub-agent count badge text, or <see langword="null"/> when zero.
	/// </summary>
	public string? SubAgentsBadge => FormatBadge("settings.sub_agents.badge.sub_agents", SubAgentCount);

	/// <summary>
	/// Gets the localized memory block count badge text, or <see langword="null"/> when zero.
	/// </summary>
	public string? MemoryBadge => FormatBadge("settings.sub_agents.badge.memory", MemoryBlockCount);

	/// <summary>
	/// Gets the tools used without approval.
	/// </summary>
	public ImmutableList<string> AllowedTools { get; }

	/// <summary>
	/// Gets the tools available to the sub-agent.
	/// </summary>
	public ImmutableList<string> AvailableTools { get; }

	/// <summary>
	/// Gets the tools disallowed for the sub-agent.
	/// </summary>
	public ImmutableList<string> DisallowedTools { get; }

	/// <summary>
	/// Gets the skills available to the sub-agent.
	/// </summary>
	public ImmutableList<string> Skills { get; }

	/// <summary>
	/// Gets the nested sub-agents available to the sub-agent.
	/// </summary>
	public ImmutableList<string> SubAgentNames { get; }

	/// <summary>
	/// Gets the memory blocks available to the sub-agent.
	/// </summary>
	public ImmutableList<SubAgentMemoryBlockItem> MemoryBlocks { get; }

	/// <summary>
	/// Gets the metadata entries of the sub-agent.
	/// </summary>
	public ImmutableList<SubAgentMetadataItem> MetadataItems { get; }

	/// <summary>
	/// Gets the system prompt of the sub-agent.
	/// </summary>
	public string SystemPrompt => _systemPrompt ??= _info.SystemPromptGetter();

	/// <summary>
	/// Gets a value indicating whether the details section has any content.
	/// </summary>
	public bool HasDetails => !string.IsNullOrWhiteSpace(SystemPrompt)
		|| AllowedTools.Count > 0 || AvailableTools.Count > 0 || DisallowedTools.Count > 0
		|| Skills.Count > 0 || SubAgentNames.Count > 0 || MemoryBlocks.Count > 0 || MetadataItems.Count > 0;

	/// <summary>
	/// Gets or sets a value indicating whether the details section is visible.
	/// </summary>
	public bool IsDetailsVisible
	{
		get => _isDetailsVisible;
		set => SetProperty(ref _isDetailsVisible, value);
	}

	/// <summary>
	/// Gets or sets the selected model full name, or an empty string for the inherited
	/// (default) model. Setting a non-empty value creates a <see cref="SubAgentChange"/>
	/// with the model override.
	/// </summary>
	public string SelectedModel
	{
		get => _change?.Model ?? _info.Model ?? string.Empty;
		set
		{
			var current = _change?.Model ?? _info.Model ?? string.Empty;
			if (current == value)
				return;

			if (string.IsNullOrEmpty(value))
			{
				if (_change != null)
				{
					_change.Model = null;
					RaisePropertyChanged(nameof(SelectedModel));
				}
				return;
			}

			EnsureChange().Model = value;
			RaisePropertyChanged(nameof(SelectedModel));
		}
	}

	/// <summary>
	/// Gets the command that toggles the details section visibility.
	/// </summary>
	public ICommand ToggleDetailsCommand { get; }

	/// <summary>
	/// Gets the command that resets the per-agent changes of this sub-agent.
	/// </summary>
	public ICommand ResetCommand { get; }

	/// <summary>
	/// Gets the command that applies a tag filter by clicking a tag chip.
	/// </summary>
	public ICommand FilterByTagCommand { get; }

	/// <summary>
	/// Gets the command that reveals the sub-agent file in the system file explorer.
	/// </summary>
	public ICommand ShowInExplorerCommand { get; }

	/// <summary>
	/// Gets the command that opens the sub-agent file with the default application.
	/// </summary>
	public ICommand OpenFileCommand { get; }

	/// <summary>
	/// Gets the command that deletes the sub-agent file after confirmation.
	/// </summary>
	public ICommand DeleteFileCommand { get; }

	private void Reset()
	{
		if (_change != null && _changes != null)
		{
			_changes.Remove(_change);
			_change = null;
			RaisePropertyChanged(nameof(Enabled));
			RaisePropertyChanged(nameof(SelectedModel));
		}
	}

	private SubAgentChange EnsureChange()
	{
		if (_change == null)
		{
			_change = new SubAgentChange
			{
				SubAgentName = Name,
				Enabled = null,
				Model = null
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
			Title = LocalizationManager.LocalizeStatic("settings.sub_agents.delete.title"),
			Description = LocalizationManager.LocalizeStaticFormat("settings.sub_agents.delete.confirm", _info.Path),
			ConfirmText = LocalizationManager.LocalizeStatic("common.delete"),
			CancelText = LocalizationManager.LocalizeStatic("common.cancel"),
			IsDanger = true
		};

		var confirmed = (bool)await DialogManager.ShowDialogAsync(confirm)!;
		if (!confirmed)
			return;

		try
		{
			File.Delete(_info.Path);
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

	private static string LocalizeMetadataKey(SubAgentMetadataType type)
	{
		var key = $"subagent.metadata.{type.ToString().ToLower()}";
		var localized = LocalizationManager.LocalizeStatic(key);
		return localized == key ? type.ToString() : localized;
	}

	private static string LocalizeSource(SubAgentSource source)
	{
		var key = $"subagent.source.{source.ToString().ToLower()}";
		var localized = LocalizationManager.LocalizeStatic(key);
		return localized == key ? source.ToString() : localized;
	}

	private static string? FormatBadge(string key, int count) =>
		count > 0 ? LocalizationManager.LocalizeStaticFormat(key, count) : null;
}
