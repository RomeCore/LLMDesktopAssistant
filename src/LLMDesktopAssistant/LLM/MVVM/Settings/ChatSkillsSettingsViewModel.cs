using System.ComponentModel;
using System.Diagnostics;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.LLM.Services.Prompting;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Prompting.Skills;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// ViewModel for the chat-level skill settings: the list of available skills with
/// search, creation and file actions, plus the skill sources group.
/// </summary>
[ViewModelFor(typeof(ChatSkillsSettingsView))]
public class ChatSkillsSettingsViewModel : ViewModelBase
{
	private readonly ISkillsetBuildingService? _skillsetBuilder;
	private readonly IExplorerOpener? _explorerOpener;
	private ImmutableList<SkillCardViewModel> _allCards = [];

	/// <summary>
	/// Gets the underlying chat skill settings.
	/// </summary>
	public ChatSkillSettings SkillSettings { get; }

	/// <summary>
	/// Gets the effective skill sources resolved by the current inheritance level.
	/// </summary>
	public SkillSourcesSettings EffectiveSources => SkillSettings.GetEffectiveSources();

	private InheritanceLevelItem _selectedSourcesInheritance;
	/// <summary>
	/// Gets or sets the inheritance level for the skill sources group.
	/// </summary>
	public InheritanceLevelItem SelectedSourcesInheritance
	{
		get => _selectedSourcesInheritance;
		set
		{
			if (SetProperty(ref _selectedSourcesInheritance, value) && value != null)
				SkillSettings.SourcesInheritance = value.Value;
		}
	}

	private string _searchText = string.Empty;
	/// <summary>
	/// Gets or sets the search text filtering the available skills by name, description and tags.
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

	private RangeObservableCollection<SkillCardViewModel> _availableSkills = [];
	/// <summary>
	/// Gets or sets the filtered list of available skills.
	/// </summary>
	public ICollection<SkillCardViewModel> AvailableSkills
	{
		get => _availableSkills;
		set
		{
			_availableSkills.Reset(value);
			RaisePropertyChanged(nameof(AvailableSkills));
		}
	}

	/// <summary>
	/// Gets the command that adds a new additional skill directory path.
	/// </summary>
	public ICommand AddDirectoryCommand { get; }

	/// <summary>
	/// Gets the command that removes an additional skill directory path.
	/// </summary>
	public ICommand RemoveDirectoryCommand { get; }

	/// <summary>
	/// Gets the command that adds a new additional skill file path.
	/// </summary>
	public ICommand AddFileCommand { get; }

	/// <summary>
	/// Gets the command that removes an additional skill file path.
	/// </summary>
	public ICommand RemoveFileCommand { get; }

	/// <summary>
	/// Gets the command that opens a folder picker for selecting a skill directory.
	/// </summary>
	public ICommand BrowseDirectoryCommand { get; }

	/// <summary>
	/// Gets the command that opens a file picker for selecting a skill file.
	/// </summary>
	public ICommand BrowseFileCommand { get; }

	/// <summary>
	/// Gets the command that opens a path in the system file explorer.
	/// </summary>
	public ICommand OpenPathCommand { get; }

	/// <summary>
	/// Gets the command that refreshes the list of available skills from disk.
	/// </summary>
	public ICommand RefreshSkillsCommand { get; }

	/// <summary>
	/// Gets the command that creates a new skill file from a template.
	/// </summary>
	public ICommand CreateSkillCommand { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatSkillsSettingsViewModel"/> class.
	/// </summary>
	/// <param name="settings">The chat skill settings.</param>
	/// <param name="skillsetBuilder">The service providing the available skills, or <see langword="null"/>.</param>
	public ChatSkillsSettingsViewModel(ChatSkillSettings settings, ISkillsetBuildingService? skillsetBuilder = null)
	{
		SkillSettings = settings;
		_skillsetBuilder = skillsetBuilder;
		_explorerOpener = ServiceRegistry.Provider.GetService<IExplorerOpener>();

		_selectedSourcesInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == settings.SourcesInheritance);
		settings.PropertyChanged += SkillSettings_PropertyChanged;

		AddDirectoryCommand = new RelayCommand(() =>
		{
			EffectiveSources.AdditionalSkillDirectories.Add(string.Empty);
		});

		RemoveDirectoryCommand = new RelayCommand<string?>(path =>
		{
			if (path != null)
				EffectiveSources.AdditionalSkillDirectories.Remove(path);
		});

		AddFileCommand = new RelayCommand(() =>
		{
			EffectiveSources.AdditionalSkillFiles.Add(string.Empty);
		});

		RemoveFileCommand = new RelayCommand<string?>(path =>
		{
			if (path != null)
				EffectiveSources.AdditionalSkillFiles.Remove(path);
		});

		BrowseDirectoryCommand = new AsyncRelayCommand<string?>(BrowseDirectoryAsync);
		BrowseFileCommand = new AsyncRelayCommand<string?>(BrowseFileAsync);
		OpenPathCommand = new RelayCommand<string?>(OpenPath);
		RefreshSkillsCommand = new RelayCommand(UpdateSkills);
		CreateSkillCommand = new AsyncRelayCommand(CreateSkillAsync);

		UpdateSkills();
	}

	private void SkillSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(ChatSkillSettings.SourcesInheritance))
			return;

		_selectedSourcesInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == SkillSettings.SourcesInheritance);
		RaisePropertyChanged(nameof(SelectedSourcesInheritance));
		RaisePropertyChanged(nameof(EffectiveSources));
	}

	/// <summary>
	/// Refreshes the list of available skills from the <see cref="ISkillsetBuildingService"/>.
	/// </summary>
	public void UpdateSkills()
	{
		_allCards = (_skillsetBuilder?.GetAvailableSkills() ?? [])
			.Select(s => new SkillCardViewModel(
				s,
				canToggle: false,
				onTagClick: tag => SearchText = tag,
				onDeleted: UpdateSkills))
			.ToImmutableList();

		ApplyFilter();
	}

	private void ApplyFilter()
	{
		var query = SearchText?.Trim() ?? string.Empty;
		IEnumerable<SkillCardViewModel> filtered = _allCards;
		if (query.Length > 0)
		{
			filtered = _allCards.Where(c =>
				c.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
				c.Description.Contains(query, StringComparison.OrdinalIgnoreCase) ||
				c.Tags.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)));
		}

		AvailableSkills = filtered.ToImmutableList();
	}

	private async Task CreateSkillAsync()
	{
		var dialog = new TextInputDialogViewModel
		{
			Title = LocalizationManager.LocalizeStatic("settings.skills.create.title"),
			Description = LocalizationManager.LocalizeStatic("settings.skills.create.description"),
			Label = LocalizationManager.LocalizeStatic("settings.skills.create.name.label"),
			Placeholder = LocalizationManager.LocalizeStatic("settings.skills.create.name.placeholder"),
			SubmitText = LocalizationManager.LocalizeStatic("common.create"),
			CancelText = LocalizationManager.LocalizeStatic("common.cancel"),
			IsRequired = true
		};

		var name = (string?)await DialogManager.ShowDialogAsync(dialog);
		if (string.IsNullOrEmpty(name))
			return;

		var toast = ServiceRegistry.Provider.GetRequiredService<IToastService>();
		if (!SkillName.IsValidSkillName(name))
		{
			toast.ShowError(LocalizationManager.LocalizeStatic("settings.skills.create.title"),
				LocalizationManager.LocalizeStatic("settings.skills.create.error.invalid_name"));
			return;
		}

		var directory = Path.Combine(Directories.Skills, name);
		var path = Path.Combine(directory, "SKILL.md");
		if (File.Exists(path))
		{
			toast.ShowError(LocalizationManager.LocalizeStatic("settings.skills.create.title"),
				LocalizationManager.LocalizeStatic("settings.skills.create.error.exists"));
			return;
		}

		try
		{
			Directory.CreateDirectory(directory);
			File.WriteAllText(path, BuildTemplate(name));
			UpdateSkills();

			toast.ShowSuccess(LocalizationManager.LocalizeStatic("settings.skills.create.success"));
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
		description: A skill that helps with specific tasks.
		---

		# {name}

		Write the instructions for this skill here.
		""";

	private async Task BrowseDirectoryAsync(string? currentPath)
	{
		var result = await App.MainTopLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
		{
			Title = LocalizationManager.LocalizeStatic("settings.skills.select_directory"),
			AllowMultiple = false
		});

		if (result.Count > 0)
		{
			var newPath = result[0].Path.LocalPath;
			ReplaceOrSetPath(EffectiveSources.AdditionalSkillDirectories, currentPath, newPath);
		}
	}

	private async Task BrowseFileAsync(string? currentPath)
	{
		var result = await App.MainTopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = LocalizationManager.LocalizeStatic("settings.skills.select_file"),
			AllowMultiple = false,
			FileTypeFilter =
			[
				new("Skill files (*.md, *.mdx)") { Patterns = ["SKILL.md", "SKILL.mdx", "*.md", "*.mdx"] },
				new("All files (*.*)") { Patterns = ["*.*"] }
			]
		});

		if (result.Count > 0)
		{
			var newPath = result[0].Path.LocalPath;
			ReplaceOrSetPath(EffectiveSources.AdditionalSkillFiles, currentPath, newPath);
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
			SkillSettings.PropertyChanged -= SkillSettings_PropertyChanged;
	}
}
