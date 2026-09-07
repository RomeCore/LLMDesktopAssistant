using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Addons.Loading;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// ViewModel for the chat-level addon settings: addon sources, additional pack paths and configurable addon packs.
/// </summary>
[ViewModelFor(typeof(ChatAddonsSettingsView))]
public class ChatAddonsSettingsViewModel : ViewModelBase
{
	private readonly IChatAddonPackLocator? _packLocator;
	private readonly IExplorerOpener? _explorerOpener;

	/// <summary>
	/// Gets the underlying chat addon settings.
	/// </summary>
	public ChatAddonSettings Settings { get; }

	/// <summary>
	/// Gets the read-only list of folders that are searched for addon packs.
	/// </summary>
	public ImmutableArray<string> SearchFolders { get; }

	// ===================================
	// === Effective values            ===
	// ===================================

	/// <summary>
	/// Gets the effective value indicating whether addons are fetched from all working directories.
	/// </summary>
	public bool EffectiveFetchFromAllWorkingDirectories => Settings.GetEffectiveFetchFromAllWorkingDirectories();

	/// <summary>
	/// Gets the effective additional pack paths collection.
	/// </summary>
	public RangeObservableCollection<string> EffectiveAdditionalPackPaths => Settings.GetEffectiveAdditionalPackPaths();

	/// <summary>
	/// Gets the effective additional addon sources dictionary by addon type.
	/// </summary>
	public ObservableDictionary<string, AddonSourcesSettings> EffectiveAddonSources => Settings.GetEffectiveAddonSources();

	/// <summary>
	/// Gets the effective addon packs settings.
	/// </summary>
	public AddonPacksSettings EffectivePacks => Settings.GetEffectivePacks();

	// ===================================
	// === Inheritance selectors       ===
	// ===================================

	private InheritanceLevelItem _selectedFetchInheritance;
	/// <summary>
	/// Gets or sets the inheritance level for the 'fetch from all working directories' setting.
	/// </summary>
	public InheritanceLevelItem SelectedFetchInheritance
	{
		get => _selectedFetchInheritance;
		set
		{
			if (SetProperty(ref _selectedFetchInheritance, value) && value != null)
			{
				Settings.FetchFromAllWorkingDirectoriesInheritance = value.Value;
				RaisePropertyChanged(nameof(EffectiveFetchFromAllWorkingDirectories));
			}
		}
	}

	private InheritanceLevelItem _selectedPackPathsInheritance;
	/// <summary>
	/// Gets or sets the inheritance level for the additional pack paths.
	/// </summary>
	public InheritanceLevelItem SelectedPackPathsInheritance
	{
		get => _selectedPackPathsInheritance;
		set
		{
			if (SetProperty(ref _selectedPackPathsInheritance, value) && value != null)
			{
				Settings.AdditionalPackPathsInheritance = value.Value;
				RaisePropertyChanged(nameof(EffectiveAdditionalPackPaths));
			}
		}
	}

	private InheritanceLevelItem _selectedAddonSourcesInheritance;
	/// <summary>
	/// Gets or sets the inheritance level for the additional addon sources.
	/// </summary>
	public InheritanceLevelItem SelectedAddonSourcesInheritance
	{
		get => _selectedAddonSourcesInheritance;
		set
		{
			if (SetProperty(ref _selectedAddonSourcesInheritance, value) && value != null)
			{
				Settings.AddonSourcesInheritance = value.Value;
				RaisePropertyChanged(nameof(EffectiveAddonSources));
				RefreshSourceItems();
			}
		}
	}

	private InheritanceLevelItem _selectedPacksInheritance;
	/// <summary>
	/// Gets or sets the inheritance level for the addon packs settings.
	/// </summary>
	public InheritanceLevelItem SelectedPacksInheritance
	{
		get => _selectedPacksInheritance;
		set
		{
			if (SetProperty(ref _selectedPacksInheritance, value) && value != null)
			{
				Settings.PacksInheritance = value.Value;
				RaisePropertyChanged(nameof(EffectivePacks));
				RefreshPacks();
			}
		}
	}

	// ===================================
	// === Configurable packs          ===
	// ===================================

	private RangeObservableCollection<AddonPackToggleItemViewModel> _configurablePacks = [];
	/// <summary>
	/// Gets the list of configurable addon packs with their enabled state.
	/// </summary>
	public RangeObservableCollection<AddonPackToggleItemViewModel> ConfigurablePacks => _configurablePacks;

	/// <summary>
	/// Gets or sets a value indicating whether packs are enabled by default.
	/// </summary>
	public bool EnablePacksByDefault
	{
		get => EffectivePacks.EnablePacksByDefault;
		set
		{
			if (EffectivePacks.EnablePacksByDefault != value)
			{
				EffectivePacks.EnablePacksByDefault = value;
				RaisePropertyChanged();
				RefreshPacks();
			}
		}
	}

	// ===================================
	// === Additional sources per type ===
	// ===================================

	private RangeObservableCollection<AddonSourceItemViewModel> _addonSourceItems = [];
	/// <summary>
	/// Gets the list of addon source groups by addon type.
	/// </summary>
	public RangeObservableCollection<AddonSourceItemViewModel> AddonSourceItems => _addonSourceItems;

	private string? _newSourceTypeName = string.Empty;
	/// <summary>
	/// Gets or sets the new addon type name used to create an additional sources group.
	/// </summary>
	public string? NewSourceTypeName
	{
		get => _newSourceTypeName;
		set => SetProperty(ref _newSourceTypeName, value);
	}

	// ===================================
	// === Commands                    ===
	// ===================================

	/// <summary>
	/// Gets the command that adds a new additional pack path.
	/// </summary>
	public ICommand AddPackPathCommand { get; }

	/// <summary>
	/// Gets the command that removes an additional pack path.
	/// </summary>
	public ICommand RemovePackPathCommand { get; }

	/// <summary>
	/// Gets the command that opens a folder picker for selecting an addon pack directory.
	/// </summary>
	public ICommand BrowsePackPathCommand { get; }

	/// <summary>
	/// Gets the command that opens a path in the system file explorer.
	/// </summary>
	public ICommand OpenPathCommand { get; }

	/// <summary>
	/// Gets the command that refreshes the list of configurable addon packs.
	/// </summary>
	public ICommand RefreshPacksCommand { get; }

	/// <summary>
	/// Gets the command that creates a new addon sources group for the entered type name.
	/// </summary>
	public ICommand AddSourceTypeCommand { get; }

	/// <summary>
	/// Gets the command that removes an addon sources group.
	/// </summary>
	public ICommand RemoveSourceTypeCommand { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatAddonsSettingsViewModel"/> class.
	/// </summary>
	/// <param name="settings">The chat addon settings.</param>
	/// <param name="packLocator">The pack locator providing configurable packs, or <see langword="null"/>.</param>
	/// <param name="foldersProvider">The provider of addon search folders, or <see langword="null"/>.</param>
	public ChatAddonsSettingsViewModel(
		ChatAddonSettings settings,
		IChatAddonPackLocator? packLocator = null,
		IAddonPackSearchFoldersProvider? foldersProvider = null)
	{
		Settings = settings;
		_packLocator = packLocator;
		_explorerOpener = ServiceRegistry.Provider.GetService<IExplorerOpener>();

		SearchFolders = foldersProvider?.GetSearchFolders().ToImmutableArray() ?? [];

		_selectedFetchInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == settings.FetchFromAllWorkingDirectoriesInheritance);
		_selectedPackPathsInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == settings.AdditionalPackPathsInheritance);
		_selectedAddonSourcesInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == settings.AddonSourcesInheritance);
		_selectedPacksInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == settings.PacksInheritance);
		settings.PropertyChanged += Settings_PropertyChanged;

		AddPackPathCommand = new RelayCommand(() => EffectiveAdditionalPackPaths.Add(string.Empty));
		RemovePackPathCommand = new RelayCommand<string?>(path =>
		{
			if (path != null)
				EffectiveAdditionalPackPaths.Remove(path);
		});

		BrowsePackPathCommand = new AsyncRelayCommand<string?>(BrowsePackPathAsync);
		OpenPathCommand = new RelayCommand<string?>(OpenPath);
		RefreshPacksCommand = new RelayCommand(RefreshPacks);
		AddSourceTypeCommand = new RelayCommand<string?>(AddSourceType);
		RemoveSourceTypeCommand = new RelayCommand<AddonSourceItemViewModel?>(RemoveSourceType);

		RefreshSourceItems();
		RefreshPacks();
	}

	private void Settings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		switch (e.PropertyName)
		{
			case nameof(ChatAddonSettings.FetchFromAllWorkingDirectoriesInheritance):
				_selectedFetchInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == Settings.FetchFromAllWorkingDirectoriesInheritance);
				RaisePropertyChanged(nameof(SelectedFetchInheritance));
				RaisePropertyChanged(nameof(EffectiveFetchFromAllWorkingDirectories));
				break;

			case nameof(ChatAddonSettings.AdditionalPackPathsInheritance):
				_selectedPackPathsInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == Settings.AdditionalPackPathsInheritance);
				RaisePropertyChanged(nameof(SelectedPackPathsInheritance));
				RaisePropertyChanged(nameof(EffectiveAdditionalPackPaths));
				break;

			case nameof(ChatAddonSettings.AddonSourcesInheritance):
				_selectedAddonSourcesInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == Settings.AddonSourcesInheritance);
				RaisePropertyChanged(nameof(SelectedAddonSourcesInheritance));
				RaisePropertyChanged(nameof(EffectiveAddonSources));
				RefreshSourceItems();
				break;

			case nameof(ChatAddonSettings.PacksInheritance):
				_selectedPacksInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == Settings.PacksInheritance);
				RaisePropertyChanged(nameof(SelectedPacksInheritance));
				RaisePropertyChanged(nameof(EffectivePacks));
				RefreshPacks();
				break;
		}
	}

	/// <summary>
	/// Refreshes the list of configurable addon packs from the pack locator.
	/// </summary>
	public void RefreshPacks()
	{
		var packs = (_packLocator?.GetConfigurablePacks() ?? [])
			.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
			.Select(p => new AddonPackToggleItemViewModel(p, () => IsPackEnabled(p), enabled => SetPackEnabled(p, enabled)))
			.ToImmutableList();

		_configurablePacks.Reset(packs);
		RaisePropertyChanged(nameof(HasPacks));
	}

	/// <summary>
	/// Gets a value indicating whether there are any configurable packs.
	/// </summary>
	public bool HasPacks => _configurablePacks.Count > 0;

	private bool IsPackEnabled(AddonPackInfo pack)
	{
		if (EffectivePacks.EnabledPacks.TryGetValue(pack.Path, out var enabled))
			return enabled;
		return EffectivePacks.EnablePacksByDefault;
	}

	private void SetPackEnabled(AddonPackInfo pack, bool enabled)
	{
		EffectivePacks.EnabledPacks[pack.Path] = enabled;
	}

	private void RefreshSourceItems()
	{
		var items = EffectiveAddonSources.Keys
			.OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
			.Select(k => new AddonSourceItemViewModel(k, EffectiveAddonSources[k]))
			.ToImmutableList();

		_addonSourceItems.Reset(items);
	}

	private void AddSourceType(string? typeName)
	{
		if (string.IsNullOrWhiteSpace(typeName))
			return;

		var type = typeName.Trim();
		EffectiveAddonSources.GetOrAdd(type, _ => new AddonSourcesSettings());
		NewSourceTypeName = string.Empty;
		RefreshSourceItems();
	}

	private void RemoveSourceType(AddonSourceItemViewModel? item)
	{
		if (item == null)
			return;

		EffectiveAddonSources.Remove(item.TypeName);
		RefreshSourceItems();
	}

	private async Task BrowsePackPathAsync(string? currentPath)
	{
		var result = await App.MainTopLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
		{
			Title = Locale.Get("addon.settings.addon_sources.type_placeholder"),
			AllowMultiple = false
		});

		if (result.Count > 0)
		{
			var newPath = result[0].Path.LocalPath;
			ReplaceOrSetPath(EffectiveAdditionalPackPaths, currentPath, newPath);
		}
	}

	private void OpenPath(string? path)
	{
		if (string.IsNullOrWhiteSpace(path))
			return;

		_explorerOpener?.OpenPath(path);
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

	/// <inheritdoc/>
	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);

		if (disposing)
		{
			Settings.PropertyChanged -= Settings_PropertyChanged;
		}
	}
}

/// <summary>
/// ViewModel representing a single configurable addon pack with its enabled state.
/// </summary>
public class AddonPackToggleItemViewModel : NotifyPropertyChanged
{
	private readonly Func<bool> _getEnabled;
	private readonly Action<bool> _setEnabled;

	/// <summary>
	/// Gets the underlying addon pack information.
	/// </summary>
	public AddonPackInfo Pack { get; }

	/// <summary>
	/// Gets the pack name.
	/// </summary>
	public string Name => Pack.NameKey?.Value ?? Pack.Name;

	/// <summary>
	/// Gets the pack description.
	/// </summary>
	public string? Description => Pack.DescriptionKey?.Value ?? Pack.Description;

	/// <summary>
	/// Gets the pack path.
	/// </summary>
	public string Path => Pack.Path;

	/// <summary>
	/// Gets the localized source display name.
	/// </summary>
	public string SourceDisplay => Locale.Get($"addon.pack.source.{Pack.Source.ToString().ToLowerInvariant()}");

	/// <summary>
	/// Gets a value indicating whether the pack manifest is invalid.
	/// </summary>
	public bool IsManifestInvalid => Pack.IsManifestValid == false;

	/// <summary>
	/// Gets or sets a value indicating whether the pack is enabled.
	/// </summary>
	public bool IsEnabled
	{
		get => _getEnabled();
		set
		{
			if (_getEnabled() != value)
			{
				_setEnabled(value);
				RaisePropertyChanged();
			}
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AddonPackToggleItemViewModel"/> class.
	/// </summary>
	/// <param name="pack">The addon pack information.</param>
	/// <param name="getEnabled">The function providing the current enabled state.</param>
	/// <param name="setEnabled">The action setting the enabled state.</param>
	public AddonPackToggleItemViewModel(AddonPackInfo pack, Func<bool> getEnabled, Action<bool> setEnabled)
	{
		Pack = pack;
		_getEnabled = getEnabled;
		_setEnabled = setEnabled;
	}
}

/// <summary>
/// ViewModel representing an additional addon sources group for a single addon type.
/// </summary>
public class AddonSourceItemViewModel : NotifyPropertyChanged
{
	/// <summary>
	/// Gets the addon type name.
	/// </summary>
	public string TypeName { get; }

	/// <summary>
	/// Gets the underlying addon sources settings.
	/// </summary>
	public AddonSourcesSettings Sources { get; }

	/// <summary>
	/// Gets the additional directories for this addon type.
	/// </summary>
	public RangeObservableCollection<string> Directories => Sources.AdditionalDirectories;

	/// <summary>
	/// Gets the additional files for this addon type.
	/// </summary>
	public RangeObservableCollection<string> Files => Sources.AdditionalFiles;

	/// <summary>
	/// Gets the command that adds a new directory entry.
	/// </summary>
	public ICommand AddDirectoryCommand { get; }

	/// <summary>
	/// Gets the command that removes a directory entry.
	/// </summary>
	public ICommand RemoveDirectoryCommand { get; }

	/// <summary>
	/// Gets the command that adds a new file entry.
	/// </summary>
	public ICommand AddFileCommand { get; }

	/// <summary>
	/// Gets the command that removes a file entry.
	/// </summary>
	public ICommand RemoveFileCommand { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="AddonSourceItemViewModel"/> class.
	/// </summary>
	/// <param name="typeName">The addon type name.</param>
	/// <param name="sources">The underlying addon sources settings.</param>
	public AddonSourceItemViewModel(string typeName, AddonSourcesSettings sources)
	{
		TypeName = typeName;
		Sources = sources;

		AddDirectoryCommand = new RelayCommand(() => Directories.Add(string.Empty));
		RemoveDirectoryCommand = new RelayCommand<string?>(path =>
		{
			if (path != null)
				Directories.Remove(path);
		});

		AddFileCommand = new RelayCommand(() => Files.Add(string.Empty));
		RemoveFileCommand = new RelayCommand<string?>(path =>
		{
			if (path != null)
				Files.Remove(path);
		});
	}
}
