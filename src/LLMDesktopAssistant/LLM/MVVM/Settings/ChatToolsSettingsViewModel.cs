using System.IO;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.LLM.Messages;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Tools.Meta;
using LLMDesktopAssistant.Utils;
using Material.Icons;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// A filter item for a meta tool language in the tools settings.
/// </summary>
public class MetaToolLanguageFilterItem
{
	/// <summary>
	/// Gets the language value, or <see langword="null"/> for "all languages".
	/// </summary>
	public ScriptLanguageType? Value { get; }

	/// <summary>
	/// Gets the localized display name.
	/// </summary>
	public string DisplayName { get; }

	public MetaToolLanguageFilterItem(ScriptLanguageType? value)
	{
		Value = value;
		var key = value is null
			? "settings.tools.meta_tools.language.all"
			: $"settings.tools.meta_tools.language.{value.ToString()!.ToLower()}";
		DisplayName = LocalizationManager.LocalizeStatic(key);

		if (DisplayName == key || string.IsNullOrEmpty(DisplayName))
			DisplayName = value?.ToString() ?? "All";
	}

	/// <summary>
	/// Gets the filter items: "all" plus every language supported by meta tool engines.
	/// </summary>
	public static ImmutableList<MetaToolLanguageFilterItem> Create(IEnumerable<IMetaToolEngine> engines)
	{
		var builder = ImmutableList.CreateBuilder<MetaToolLanguageFilterItem>();
		builder.Add(new MetaToolLanguageFilterItem(null));
		foreach (var language in engines.Select(e => e.Language).Distinct().OrderBy(l => l.ToString()))
			builder.Add(new MetaToolLanguageFilterItem(language));
		return builder.ToImmutable();
	}
}

/// <summary>
/// A filter item for the meta tool scope in the tools settings.
/// </summary>
public class MetaToolScopeFilterItem
{
	/// <summary>
	/// Gets the scope value, or <see langword="null"/> for "all scopes".
	/// </summary>
	public MetaToolSource? Value { get; }

	/// <summary>
	/// Gets the localized display name.
	/// </summary>
	public string DisplayName { get; }

	public MetaToolScopeFilterItem(MetaToolSource? value)
	{
		Value = value;
		var key = value is null
			? "settings.tools.meta_tools.scope.all"
			: $"settings.tools.meta_tools.scope.{value.ToString()!.ToLower()}";
		DisplayName = LocalizationManager.LocalizeStatic(key);

		if (DisplayName == key || string.IsNullOrEmpty(DisplayName))
			DisplayName = value?.ToString() ?? "All";
	}

	/// <summary>
	/// Gets all scope filter items.
	/// </summary>
	public static ImmutableList<MetaToolScopeFilterItem> All { get; } =
	[
		new(null),
		new(MetaToolSource.UserProfile),
		new(MetaToolSource.WorkingDirectory),
		new(MetaToolSource.Custom)
	];
}

/// <summary>
/// ViewModel for global chat tools settings (without agent-specific policy).
/// The agent tool policy is configured in <see cref="Agents.AgentToolSettingsViewModel"/>.
/// </summary>
[ViewModelFor(typeof(ChatToolsSettingsView))]
public class ChatToolsSettingsViewModel : ViewModelBase
{
	private readonly IMetaToolManagementService _metaToolManager;
	private readonly IMetaToolParser _parser;
	private readonly IReadOnlyList<IMetaToolEngine> _engines;
	private readonly IExplorerOpener? _explorerOpener;
	private ImmutableList<MetaToolCardViewModel> _allCards = [];
	private string _searchText = string.Empty;
	private MetaToolLanguageFilterItem? _selectedLanguageFilter;
	private MetaToolScopeFilterItem? _selectedScopeFilter;
	private InheritanceLevelItem _selectedSourcesInheritance;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatToolsSettingsViewModel"/> class.
	/// </summary>
	/// <param name="settings">The chat tool settings to edit.</param>
	/// <param name="metaToolManager">The meta tool management service used to list, create and edit tools.</param>
	/// <param name="parser">The meta tool parser used to parse tool scripts.</param>
	/// <param name="engines">The meta tool engines registered in the chat container.</param>
	public ChatToolsSettingsViewModel(ChatToolSettings settings, IMetaToolManagementService metaToolManager,
		IMetaToolParser parser, IEnumerable<IMetaToolEngine> engines)
	{
		ToolSettings = settings;
		_metaToolManager = metaToolManager;
		_parser = parser;
		_engines = engines.ToArray();
		_explorerOpener = ServiceRegistry.Provider.GetService<IExplorerOpener>();

		_selectedSourcesInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == settings.SourcesInheritance);
		settings.PropertyChanged += ToolSettings_PropertyChanged;

		LanguageFilters = MetaToolLanguageFilterItem.Create(_engines);
		SelectedLanguageFilter = LanguageFilters[0];
		ScopeFilters = MetaToolScopeFilterItem.All;
		SelectedScopeFilter = ScopeFilters[0];

		RefreshMetaToolsCommand = new RelayCommand(UpdateMetaTools);
		CreateMetaToolCommand = new AsyncRelayCommand(CreateMetaToolAsync);
		OpenMetaToolsFolderCommand = new RelayCommand(OpenMetaToolsFolder);

		AddDirectoryCommand = new RelayCommand(AddDirectory);
		RemoveDirectoryCommand = new RelayCommand<string?>(RemoveDirectory);
		AddFileCommand = new RelayCommand(AddFile);
		RemoveFileCommand = new RelayCommand<string?>(RemoveFile);
		BrowseDirectoryCommand = new AsyncRelayCommand<string?>(BrowseDirectoryAsync);
		BrowseFileCommand = new AsyncRelayCommand<string?>(BrowseFileAsync);
		OpenPathCommand = new RelayCommand<string?>(OpenPath);

		UpdateMetaTools();
	}

	/// <summary>
	/// Gets the underlying chat tool settings.
	/// </summary>
	public ChatToolSettings ToolSettings { get; }

	/// <summary>
	/// Gets the effective meta tool sources (including inherited values).
	/// </summary>
	public MetaToolSourcesSettings EffectiveSources => ToolSettings.GetEffectiveSources();

	/// <summary>
	/// Gets or sets the inheritance level for the meta tool sources group.
	/// </summary>
	public InheritanceLevelItem SelectedSourcesInheritance
	{
		get => _selectedSourcesInheritance;
		set
		{
			if (SetProperty(ref _selectedSourcesInheritance, value) && value != null)
				ToolSettings.SourcesInheritance = value.Value;
		}
	}

	private void ToolSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(ChatToolSettings.SourcesInheritance))
			return;

		_selectedSourcesInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == ToolSettings.SourcesInheritance);
		RaisePropertyChanged(nameof(SelectedSourcesInheritance));
		RaisePropertyChanged(nameof(EffectiveSources));
	}

	// ─────────────────────────── Meta tools list ───────────────────────────

	/// <summary>
	/// Gets the meta tool cards filtered by the current search text and filters.
	/// </summary>
	public RangeObservableCollection<MetaToolCardViewModel> MetaTools { get; } = [];

	/// <summary>
	/// Gets or sets the search text used to filter meta tools by name, title, description and category.
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

	/// <summary>
	/// Gets the language filter items.
	/// </summary>
	public ImmutableList<MetaToolLanguageFilterItem> LanguageFilters { get; }

	/// <summary>
	/// Gets or sets the selected language filter.
	/// </summary>
	public MetaToolLanguageFilterItem? SelectedLanguageFilter
	{
		get => _selectedLanguageFilter;
		set
		{
			if (SetProperty(ref _selectedLanguageFilter, value))
				ApplyFilter();
		}
	}

	/// <summary>
	/// Gets the scope filter items.
	/// </summary>
	public ImmutableList<MetaToolScopeFilterItem> ScopeFilters { get; }

	/// <summary>
	/// Gets or sets the selected scope filter.
	/// </summary>
	public MetaToolScopeFilterItem? SelectedScopeFilter
	{
		get => _selectedScopeFilter;
		set
		{
			if (SetProperty(ref _selectedScopeFilter, value))
				ApplyFilter();
		}
	}

	/// <summary>
	/// Gets the command that re-reads the meta tool files from disk.
	/// </summary>
	public ICommand RefreshMetaToolsCommand { get; }

	/// <summary>
	/// Gets the command that creates a new meta tool.
	/// </summary>
	public ICommand CreateMetaToolCommand { get; }

	/// <summary>
	/// Gets the command that opens the application meta tools folder in the system explorer.
	/// </summary>
	public ICommand OpenMetaToolsFolderCommand { get; }

	// ─────────────────────────── Meta tool sources ─────────────────────────

	/// <summary>
	/// Gets the command that adds a new empty additional directory entry.
	/// </summary>
	public ICommand AddDirectoryCommand { get; }

	/// <summary>
	/// Gets the command that removes an additional directory entry.
	/// </summary>
	public ICommand RemoveDirectoryCommand { get; }

	/// <summary>
	/// Gets the command that adds a new empty additional file entry.
	/// </summary>
	public ICommand AddFileCommand { get; }

	/// <summary>
	/// Gets the command that removes an additional file entry.
	/// </summary>
	public ICommand RemoveFileCommand { get; }

	/// <summary>
	/// Gets the command that opens a folder picker for an additional directory entry.
	/// </summary>
	public ICommand BrowseDirectoryCommand { get; }

	/// <summary>
	/// Gets the command that opens a file picker for an additional file entry.
	/// </summary>
	public ICommand BrowseFileCommand { get; }

	/// <summary>
	/// Gets the command that opens a path in the system explorer.
	/// </summary>
	public ICommand OpenPathCommand { get; }

	// ─────────────────────────── Implementation ────────────────────────────

	private void UpdateMetaTools()
	{
		_allCards = _metaToolManager?.ListTools()
			.Select(t => new MetaToolCardViewModel(t, _metaToolManager,
				onChanged: UpdateMetaTools,
				onEdit: card => _ = EditMetaToolAsync(card)))
			.ToImmutableList() ?? [];

		ApplyFilter();
	}

	private void ApplyFilter()
	{
		var query = SearchText.Trim();
		var language = SelectedLanguageFilter?.Value;
		var scope = SelectedScopeFilter?.Value;

		var filtered = _allCards.Where(card =>
		{
			if (language is not null && card.Language != language)
				return false;
			if (scope is not null && card.Source != scope)
				return false;
			if (string.IsNullOrEmpty(query))
				return true;

			return card.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
				|| card.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
				|| card.Description.Contains(query, StringComparison.OrdinalIgnoreCase)
				|| card.Category.Contains(query, StringComparison.OrdinalIgnoreCase);
		});

		MetaTools.Reset(filtered);
	}

	private async Task EditMetaToolAsync(MetaToolCardViewModel card)
	{
		var dialog = new MetaToolEditorDialogViewModel(card.Info, _metaToolManager, _parser, _engines);
#pragma warning disable CS8605 // Unboxing is safe: the dialog always returns bool.
		var result = (bool)await DialogManager.ShowDialogAsync(dialog);
#pragma warning restore CS8605
		if (result)
			UpdateMetaTools();
	}

	private async Task CreateMetaToolAsync()
	{
		if (_metaToolManager is null)
			return;

		var dialog = new CreateMetaToolDialogViewModel(_engines);
		await DialogManager.ShowDialogAsync(dialog);
		if (!await dialog.Result)
			return;

		var (name, language) = dialog.GetResult();
		if (string.IsNullOrEmpty(name) || language is null)
			return;

		try
		{
			if (!ToolName.CheckValid(name))
			{
				ShowError(LocalizationManager.LocalizeStatic("settings.tools.meta_tools.create.error.invalid_name"));
				return;
			}

			var engine = _engines.FirstOrDefault(e => e.Language == language.Value);
			if (engine is null)
			{
				ShowError(LocalizationManager.LocalizeStaticFormat("settings.tools.meta_tools.create.error.no_engine", language.Value));
				return;
			}

			var filePath = Path.Combine(Directories.Metatools, name + engine.Descriptor.MainExtension);
			if (File.Exists(filePath))
			{
				ShowError(LocalizationManager.LocalizeStatic("settings.tools.meta_tools.create.error.exists"));
				return;
			}

			Directory.CreateDirectory(Directories.Metatools);
			File.WriteAllText(filePath, engine.Descriptor.Template);

			var info = _metaToolManager.ListTools().FirstOrDefault(t => t.Name == name);
			if (info is null)
				return;

			var editorDialog = new MetaToolEditorDialogViewModel(info, _metaToolManager, _parser, _engines);
#pragma warning disable CS8605 // Unboxing is safe: the dialog always returns bool.
			var result = (bool)await DialogManager.ShowDialogAsync(editorDialog);
#pragma warning restore CS8605
			if (result)
				UpdateMetaTools();
		}
		catch (Exception ex)
		{
			ShowError(ex.Message);
		}
	}

	private void OpenMetaToolsFolder()
	{
		if (!Directory.Exists(Directories.Metatools))
			Directory.CreateDirectory(Directories.Metatools);
		_explorerOpener?.OpenPath(Directories.Metatools);
	}

	private void AddDirectory() => EffectiveSources.AdditionalMetaToolDirectories.Add(string.Empty);

	private void RemoveDirectory(string? path)
	{
		if (path is not null)
			EffectiveSources.AdditionalMetaToolDirectories.Remove(path);
	}

	private void AddFile() => EffectiveSources.AdditionalMetaToolFiles.Add(string.Empty);

	private void RemoveFile(string? path)
	{
		if (path is not null)
			EffectiveSources.AdditionalMetaToolFiles.Remove(path);
	}

	private async Task BrowseDirectoryAsync(string? currentPath)
	{
		var result = await App.MainTopLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
		{
			Title = LocalizationManager.LocalizeStatic("settings.tools.meta_tools.select_directory"),
			AllowMultiple = false
		});

		if (result.Count > 0)
		{
			var newPath = result[0].Path.LocalPath;
			ReplaceOrSetPath(EffectiveSources.AdditionalMetaToolDirectories, currentPath, newPath);
		}
	}

	private async Task BrowseFileAsync(string? currentPath)
	{
		var result = await App.MainTopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = LocalizationManager.LocalizeStatic("settings.tools.meta_tools.select_file"),
			AllowMultiple = false,
			FileTypeFilter =
			[
				new("Meta tool files (*.lua, *.py, *.csx)") { Patterns = ["*.lua", "*.alua", "*.py", "*.csx"] },
				new("All files (*.*)") { Patterns = ["*.*"] }
			]
		});

		if (result.Count > 0)
		{
			var newPath = result[0].Path.LocalPath;
			ReplaceOrSetPath(EffectiveSources.AdditionalMetaToolFiles, currentPath, newPath);
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
		if (!string.IsNullOrEmpty(path))
			_explorerOpener?.OpenPath(path);
	}

	private static void ShowError(string message)
	{
		ServiceRegistry.Provider.GetRequiredService<IToastService>()
			.ShowError(LocalizationManager.LocalizeStatic("common.error"), message);
	}
}
