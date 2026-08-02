using Avalonia.Platform.Storage;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Messages;
using LLMDesktopAssistant.LLM.Services.Prompting;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Prompting.Skills;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// A lightweight view model wrapping a <see cref="SkillInfo"/> for display in the settings UI,
/// providing computed diagnostic flag infos.
/// </summary>
public class SkillInfoItemViewModel
{
	/// <summary>
	/// The underlying <see cref="SkillInfo"/>.
	/// </summary>
	public SkillInfo SkillInfo { get; }

	/// <summary>
	/// Gets the name of the skill.
	/// </summary>
	public string Name => SkillInfo.Name;

	/// <summary>
	/// Gets the description of the skill.
	/// </summary>
	public string Description => SkillInfo.Description;

	/// <summary>
	/// Gets the file path of the skill, if applicable.
	/// </summary>
	public string? Path => SkillInfo.Path;

	/// <summary>
	/// Gets whether this skill is enabled.
	/// </summary>
	public bool Enabled => SkillInfo.Enabled;

	/// <summary>
	/// Gets the list of diagnostic flag infos for display in the UI.
	/// </summary>
	public ImmutableList<SkillDiagnosticFlagInfo> DiagnosticFlags { get; }

	public SkillInfoItemViewModel(SkillInfo skillInfo)
	{
		SkillInfo = skillInfo;
		DiagnosticFlags = SkillDiagnosticFlagInfo.CreateFromDiagnostic(skillInfo.Diagnostic);
	}
}

/// <summary>
/// ViewModel for global chat skills settings.
/// </summary>
[ViewModelFor(typeof(ChatSkillsSettingsView))]
public class ChatSkillsSettingsViewModel : ViewModelBase
{
	private readonly ISkillsetBuildingService? _skillsetBuilder;
	private readonly IExplorerOpener? _explorerOpener;

	public ChatSkillSettings SkillSettings { get; }

	/// <summary>
	/// Command to open a folder picker dialog for selecting a skill directory.
	/// </summary>
	public ICommand BrowseDirectoryCommand { get; }

	/// <summary>
	/// Command to open a file picker dialog for selecting a SKILL.md / SKILL.mdx file.
	/// </summary>
	public ICommand BrowseFileCommand { get; }

	/// <summary>
	/// Command to open a path in the system file explorer.
	/// </summary>
	public ICommand OpenPathCommand { get; }

	private RangeObservableCollection<SkillInfoItemViewModel> _availableSkills = [];
	/// <summary>
	/// Gets or sets the list of available skills discovered by the skill locator and loader.
	/// </summary>
	public ICollection<SkillInfoItemViewModel> AvailableSkills
	{
		get => _availableSkills;
		set
		{
			_availableSkills.Reset(value);
			RaisePropertyChanged(nameof(AvailableSkills));
		}
	}

	/// <summary>
	/// Command to add a new additional skill directory path.
	/// </summary>
	public ICommand AddDirectoryCommand { get; }

	/// <summary>
	/// Command to remove an additional skill directory path.
	/// </summary>
	public ICommand RemoveDirectoryCommand { get; }

	/// <summary>
	/// Command to add a new additional skill file path.
	/// </summary>
	public ICommand AddFileCommand { get; }

	/// <summary>
	/// Command to remove an additional skill file path.
	/// </summary>
	public ICommand RemoveFileCommand { get; }

	/// <summary>
	/// Command to refresh the list of available skills from disk.
	/// </summary>
	public ICommand RefreshSkillsCommand { get; }

	public ChatSkillsSettingsViewModel(ChatSkillSettings settings, ISkillsetBuildingService? skillsetBuilder = null)
	{
		SkillSettings = settings;
		_skillsetBuilder = skillsetBuilder;
		_explorerOpener = ServiceRegistry.Provider.GetService<IExplorerOpener>();

		AddDirectoryCommand = new RelayCommand(() =>
		{
			SkillSettings.AdditionalSkillDirectories.Add(string.Empty);
		});

		RemoveDirectoryCommand = new RelayCommand<string?>(path =>
		{
			if (path != null)
				SkillSettings.AdditionalSkillDirectories.Remove(path);
		});

		AddFileCommand = new RelayCommand(() =>
		{
			SkillSettings.AdditionalSkillFiles.Add(string.Empty);
		});

		RemoveFileCommand = new RelayCommand<string?>(path =>
		{
			if (path != null)
				SkillSettings.AdditionalSkillFiles.Remove(path);
		});

		BrowseDirectoryCommand = new AsyncRelayCommand<string?>(BrowseDirectoryAsync);
		BrowseFileCommand = new AsyncRelayCommand<string?>(BrowseFileAsync);
		OpenPathCommand = new RelayCommand<string?>(OpenPath);

		RefreshSkillsCommand = new RelayCommand(UpdateSkills);
	}

	private async Task BrowseDirectoryAsync(string? currentPath)
	{
		var result = await App.MainTopLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
		{
			Title = LocalizationManager.LocalizeStatic("settings-skills_select_directory"),
			AllowMultiple = false
		});

		if (result.Count > 0)
		{
			var newPath = result[0].Path.LocalPath;
			// Find the directory entry and replace it
			ReplaceOrSetPath(SkillSettings.AdditionalSkillDirectories, currentPath, newPath);
		}
	}

	private async Task BrowseFileAsync(string? currentPath)
	{
		var result = await App.MainTopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = LocalizationManager.LocalizeStatic("settings-skills_select_file"),
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
			ReplaceOrSetPath(SkillSettings.AdditionalSkillFiles, currentPath, newPath);
		}
	}

	private static void ReplaceOrSetPath(RangeObservableCollection<string> collection, string? oldValue, string newValue)
	{
		if (string.IsNullOrEmpty(oldValue))
		{
			// No existing path — add as new entry (replace first empty entry, or add)
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

	/// <summary>
	/// Refreshes the list of available skills from the <see cref="ISkillsetBuildingService"/>.
	/// </summary>
	public void UpdateSkills()
	{
		if (_skillsetBuilder != null)
			AvailableSkills = _skillsetBuilder.GetAvailableSkills()
				.Select(s => new SkillInfoItemViewModel(s))
				.ToImmutableList();
	}
}
