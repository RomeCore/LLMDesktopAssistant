using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Agents.Memory;
using LLMDesktopAssistant.Agents.SubAgents;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.LLM.Services.Prompting;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// ViewModel for the chat-level sub-agent settings: the list of available sub-agents with
/// search, creation and file actions, plus the sub-agent sources group.
/// </summary>
[ViewModelFor(typeof(ChatSubAgentsSettingsView))]
public class ChatSubAgentsSettingsViewModel : ViewModelBase
{
	private readonly ISubAgentSetBuildingService _subAgentSetBuilder;
	private readonly ISkillsetBuildingService _skillsetBuilder;
	private readonly IExplorerOpener? _explorerOpener;
	private ImmutableList<SubAgentCardViewModel> _allCards = [];

	/// <summary>
	/// Gets the underlying chat sub-agent settings.
	/// </summary>
	public ChatSubAgentSettings SubAgentSettings { get; }

	/// <summary>
	/// Gets the effective sub-agent sources resolved by the current inheritance level.
	/// </summary>
	public SubAgentSourcesSettings EffectiveSources => SubAgentSettings.GetEffectiveSources();

	private InheritanceLevelItem _selectedSourcesInheritance;
	/// <summary>
	/// Gets or sets the inheritance level for the sub-agent sources group.
	/// </summary>
	public InheritanceLevelItem SelectedSourcesInheritance
	{
		get => _selectedSourcesInheritance;
		set
		{
			if (SetProperty(ref _selectedSourcesInheritance, value) && value != null)
				SubAgentSettings.SourcesInheritance = value.Value;
		}
	}

	private string _searchText = string.Empty;
	/// <summary>
	/// Gets or sets the search text filtering the available sub-agents by name, description and tags.
	/// </summary>
	public string SearchText
	{
		get => _searchText;
		set
		{
			if (SetProperty(ref _searchText, value))
				ApplyFilter();
		}
	}

	private RangeObservableCollection<SubAgentCardViewModel> _availableSubAgents = [];
	/// <summary>
	/// Gets or sets the filtered list of available sub-agents.
	/// </summary>
	public ICollection<SubAgentCardViewModel> AvailableSubAgents
	{
		get => _availableSubAgents;
		set
		{
			_availableSubAgents.Reset(value);
			RaisePropertyChanged(nameof(AvailableSubAgents));
		}
	}

	/// <summary>
	/// Gets the command that adds a new additional sub-agent directory path.
	/// </summary>
	public ICommand AddDirectoryCommand { get; }

	/// <summary>
	/// Gets the command that removes an additional sub-agent directory path.
	/// </summary>
	public ICommand RemoveDirectoryCommand { get; }

	/// <summary>
	/// Gets the command that adds a new additional sub-agent file path.
	/// </summary>
	public ICommand AddFileCommand { get; }

	/// <summary>
	/// Gets the command that removes an additional sub-agent file path.
	/// </summary>
	public ICommand RemoveFileCommand { get; }

	/// <summary>
	/// Gets the command that opens a folder picker for selecting a sub-agent directory.
	/// </summary>
	public ICommand BrowseDirectoryCommand { get; }

	/// <summary>
	/// Gets the command that opens a file picker for selecting a sub-agent file.
	/// </summary>
	public ICommand BrowseFileCommand { get; }

	/// <summary>
	/// Gets the command that opens a path in the system file explorer.
	/// </summary>
	public ICommand OpenPathCommand { get; }

	/// <summary>
	/// Gets the command that refreshes the list of available sub-agents from disk.
	/// </summary>
	public ICommand RefreshSubAgentsCommand { get; }

	/// <summary>
	/// Gets the command that creates a new sub-agent file from a template.
	/// </summary>
	public ICommand CreateSubAgentCommand { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatSubAgentsSettingsViewModel"/> class.
	/// </summary>
	/// <param name="settings">The chat sub-agent settings.</param>
	/// <param name="subAgentSetBuilder">The service providing the available sub-agents.</param>
	/// <param name="skillsetBuilder">The service providing the available skills for link checking.</param>
	public ChatSubAgentsSettingsViewModel(ChatSubAgentSettings settings,
		ISubAgentSetBuildingService subAgentSetBuilder, ISkillsetBuildingService skillsetBuilder)
	{
		SubAgentSettings = settings;
		_subAgentSetBuilder = subAgentSetBuilder;
		_skillsetBuilder = skillsetBuilder;
		_explorerOpener = ServiceRegistry.Provider.GetService<IExplorerOpener>();

		_selectedSourcesInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == settings.SourcesInheritance);
		settings.PropertyChanged += SubAgentSettings_PropertyChanged;

		AddDirectoryCommand = new RelayCommand(() =>
		{
			EffectiveSources.AdditionalSubAgentDirectories.Add(string.Empty);
		});

		RemoveDirectoryCommand = new RelayCommand<string?>(path =>
		{
			if (path != null)
				EffectiveSources.AdditionalSubAgentDirectories.Remove(path);
		});

		AddFileCommand = new RelayCommand(() =>
		{
			EffectiveSources.AdditionalSubAgentFiles.Add(string.Empty);
		});

		RemoveFileCommand = new RelayCommand<string?>(path =>
		{
			if (path != null)
				EffectiveSources.AdditionalSubAgentFiles.Remove(path);
		});

		BrowseDirectoryCommand = new AsyncRelayCommand<string?>(BrowseDirectoryAsync);
		BrowseFileCommand = new AsyncRelayCommand<string?>(BrowseFileAsync);
		OpenPathCommand = new RelayCommand<string?>(OpenPath);
		RefreshSubAgentsCommand = new RelayCommand(UpdateSubAgents);
		CreateSubAgentCommand = new AsyncRelayCommand(CreateSubAgentAsync);

		UpdateSubAgents();
	}

	private void SubAgentSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(ChatSubAgentSettings.SourcesInheritance))
			return;

		_selectedSourcesInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == SubAgentSettings.SourcesInheritance);
		RaisePropertyChanged(nameof(SelectedSourcesInheritance));
		RaisePropertyChanged(nameof(EffectiveSources));
	}

	/// <summary>
	/// Refreshes the list of available sub-agents from the <see cref="ISubAgentSetBuildingService"/>.
	/// </summary>
	public void UpdateSubAgents()
	{
		var subAgents = _subAgentSetBuilder.GetAvailableSubAgents().ToList();
		var subAgentNames = subAgents.Select(s => s.Name).ToHashSet();
		var skillNames = _skillsetBuilder.GetAvailableSkills().Select(s => s.Name).ToHashSet();
		var memoryBlockNames = SettingsManager.GetCategory<MemoryBlock>().GetAll().Select(kvp => kvp.Value.Name).ToHashSet();

		_allCards = subAgents
			.Select(s => new SubAgentCardViewModel(
				s,
				canToggle: false,
				linkIssues: SubAgentLinkChecker.Check(s, skillNames, subAgentNames, memoryBlockNames),
				onTagClick: tag => SearchText = tag,
				onDeleted: UpdateSubAgents))
			.ToImmutableList();

		ApplyFilter();
	}

	private void ApplyFilter()
	{
		var query = SearchText?.Trim() ?? string.Empty;
		IEnumerable<SubAgentCardViewModel> filtered = _allCards;
		if (query.Length > 0)
		{
			filtered = _allCards.Where(c =>
				c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
				c.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
				c.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)));
		}

		AvailableSubAgents = filtered.ToImmutableList();
	}

	private async Task CreateSubAgentAsync()
	{
		var dialog = new TextInputDialogViewModel
		{
			Title = LocalizationManager.LocalizeStatic("settings.sub_agents.create.title"),
			Description = LocalizationManager.LocalizeStatic("settings.sub_agents.create.description"),
			Label = LocalizationManager.LocalizeStatic("settings.sub_agents.create.name.label"),
			Placeholder = LocalizationManager.LocalizeStatic("settings.sub_agents.create.name.placeholder"),
			SubmitText = LocalizationManager.LocalizeStatic("common.create"),
			CancelText = LocalizationManager.LocalizeStatic("common.cancel"),
			IsRequired = true
		};

		await DialogManager.ShowDialogAsync(dialog);
		var name = await dialog.Result;
		if (string.IsNullOrEmpty(name))
			return;

		var toast = ServiceRegistry.Provider.GetRequiredService<IToastService>();
		if (!SubAgentName.IsValidSubAgentName(name))
		{
			toast.ShowError(LocalizationManager.LocalizeStatic("settings.sub_agents.create.title"),
				LocalizationManager.LocalizeStatic("settings.sub_agents.create.error.invalid_name"));
			return;
		}

		var path = Path.Combine(Directories.Agents, $"{name}.md");
		if (File.Exists(path))
		{
			toast.ShowError(LocalizationManager.LocalizeStatic("settings.sub_agents.create.title"),
				LocalizationManager.LocalizeStatic("settings.sub_agents.create.error.exists"));
			return;
		}

		try
		{
			Directory.CreateDirectory(Directories.Agents);
			File.WriteAllText(path, BuildTemplate(name));
			UpdateSubAgents();

			toast.ShowSuccess(LocalizationManager.LocalizeStatic("settings.sub_agents.create.success"));
			Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
		}
		catch (Exception ex)
		{
			toast.ShowError(LocalizationManager.LocalizeStatic("common.error"), ex.Message);
		}
	}

	private static string BuildTemplate(string name) => $"""
		---
		name: {name}
		description: A sub-agent that helps with specific tasks.
		---

		# {name}

		Write the instructions for this sub-agent here.
		""";

	private async Task BrowseDirectoryAsync(string? currentPath)
	{
		var result = await App.MainTopLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
		{
			Title = LocalizationManager.LocalizeStatic("settings.sub_agents.select_directory"),
			AllowMultiple = false
		});

		if (result.Count > 0)
		{
			var newPath = result[0].Path.LocalPath;
			ReplaceOrSetPath(EffectiveSources.AdditionalSubAgentDirectories, currentPath, newPath);
		}
	}

	private async Task BrowseFileAsync(string? currentPath)
	{
		var result = await App.MainTopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = LocalizationManager.LocalizeStatic("settings.sub_agents.select_file"),
			AllowMultiple = false,
			FileTypeFilter =
			[
				new("Sub-agent files (*.md, *.mdx)") { Patterns = ["*.md", "*.mdx"] },
				new("All files (*.*)") { Patterns = ["*.*"] }
			]
		});

		if (result.Count > 0)
		{
			var newPath = result[0].Path.LocalPath;
			ReplaceOrSetPath(EffectiveSources.AdditionalSubAgentFiles, currentPath, newPath);
		}
	}

	private static void ReplaceOrSetPath(RangeObservableCollection<string> collection, string? oldValue, string newValue)
	{
		if (string.IsNullOrEmpty(oldValue))
		{
			for (int i = 0; i < collection.Count; i++)
			{
				if (string.IsNullOrEmpty(collection[i]))
				{
					collection[i] = newValue;
					return;
				}
			}
			collection.Add(newValue);
		}
		else
		{
			var index = collection.IndexOf(oldValue);
			if (index >= 0)
				collection[index] = newValue;
			else
				collection.Add(newValue);
		}
	}

	private void OpenPath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return;

		_explorerOpener?.OpenPath(path);
	}

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (disposing)
			SubAgentSettings.PropertyChanged -= SubAgentSettings_PropertyChanged;
	}
}
