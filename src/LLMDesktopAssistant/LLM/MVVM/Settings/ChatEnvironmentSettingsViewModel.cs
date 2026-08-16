using System.ComponentModel;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// ViewModel for the Environment settings tab.
	/// Manages working directories, directory access rules, and Python environment paths.
	/// All groups are resolved through their effective (inherited) scope, selected via
	/// the inheritance level combo boxes in the view.
	/// </summary>
	[ViewModelFor(typeof(ChatEnvironmentSettingsView))]
	public class ChatEnvironmentSettingsViewModel : ViewModelBase
	{
		private readonly List<IScriptEngineEnvConfigurationProvider> _scriptEngineConfigProviders;
		private readonly IExplorerOpener? _explorerOpener;

		/// <summary>
		/// Gets the underlying environment settings.
		/// </summary>
		public ChatEnvironmentSettings EnvironmentSettings { get; }
		
		/// <summary>
		/// Gets the effective working directories configuration resolved by the current inheritance level.
		/// </summary>
		public WorkingDirectoriesSettings EffectiveWorkingDirectories => EnvironmentSettings.GetEffectiveWorkingDirectories();

		/// <summary>
		/// Gets the effective directory access rules resolved by the current inheritance level.
		/// </summary>
		public RangeObservableCollection<DirectoryAccessSetting> EffectiveDirectoryAccessRules => EnvironmentSettings.GetEffectiveDirectoryAccessRules();

		/// <summary>
		/// Gets the effective additional environment settings resolved by the current inheritance level.
		/// </summary>
		public RangeObservableCollection<AdditionalEnvironmentSetting> EffectiveAdditionalSettings => EnvironmentSettings.GetEffectiveAdditionalSettings();

		private InheritanceLevelItem _selectedWorkingDirectoriesInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the working directories group.
		/// </summary>
		public InheritanceLevelItem SelectedWorkingDirectoriesInheritance
		{
			get => _selectedWorkingDirectoriesInheritance;
			set
			{
				if (SetProperty(ref _selectedWorkingDirectoriesInheritance, value) && value != null)
					EnvironmentSettings.WorkingDirectoriesInheritance = value.Value;
			}
		}

		private InheritanceLevelItem _selectedDirectoryAccessRulesInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the directory access rules group.
		/// </summary>
		public InheritanceLevelItem SelectedDirectoryAccessRulesInheritance
		{
			get => _selectedDirectoryAccessRulesInheritance;
			set
			{
				if (SetProperty(ref _selectedDirectoryAccessRulesInheritance, value) && value != null)
					EnvironmentSettings.DirectoryAccessRulesInheritance = value.Value;
			}
		}

		private InheritanceLevelItem _selectedAdditionalSettingsInheritance;
		/// <summary>
		/// Gets or sets the inheritance level for the additional environment settings group.
		/// </summary>
		public InheritanceLevelItem SelectedAdditionalSettingsInheritance
		{
			get => _selectedAdditionalSettingsInheritance;
			set
			{
				if (SetProperty(ref _selectedAdditionalSettingsInheritance, value) && value != null)
					EnvironmentSettings.AdditionalSettingsInheritance = value.Value;
			}
		}

		private ImmutableList<ScriptEnvironmentSettingsItemViewModel> _additionalEnvironmentSettings = [];
		/// <summary>
		/// Gets the additional environment settings view models built from the effective settings.
		/// </summary>
		public ImmutableList<ScriptEnvironmentSettingsItemViewModel> AdditionalEnvironmentSettings
		{
			get => _additionalEnvironmentSettings;
			private set => SetProperty(ref _additionalEnvironmentSettings, value);
		}

		public IRelayCommand AddWorkingDirectoryCommand { get; }
		public IRelayCommand<WorkingDirectorySetting> RemoveWorkingDirectoryCommand { get; }
		public IRelayCommand<WorkingDirectorySetting> MoveWorkingDirectoryUpCommand { get; }
		public IRelayCommand<WorkingDirectorySetting> MoveWorkingDirectoryDownCommand { get; }
		public IRelayCommand<WorkingDirectorySetting> BrowseWorkingDirectoryPathCommand { get; }
		public IRelayCommand SetDefaultWorkingDirectoryActiveCommand { get; }
		public IRelayCommand<WorkingDirectorySetting> SetActiveWorkingDirectoryCommand { get; }

		public IRelayCommand AddDirectoryAccessRuleCommand { get; }
		public IRelayCommand<DirectoryAccessSetting> RemoveDirectoryAccessRuleCommand { get; }
		public IRelayCommand<DirectoryAccessSetting> MoveDirectoryAccessRuleUpCommand { get; }
		public IRelayCommand<DirectoryAccessSetting> MoveDirectoryAccessRuleDownCommand { get; }
		public IRelayCommand<DirectoryAccessSetting> BrowseDirectoryAccessRulePathCommand { get; }

		public IRelayCommand<string?> OpenDirectoryCommand { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ChatEnvironmentSettingsViewModel"/> class.
		/// </summary>
		/// <param name="settings">The environment settings to edit.</param>
		/// <param name="scriptEngineConfigProviders">The script engine environment configuration providers.</param>
		/// <param name="explorerOpener">The explorer opener used to reveal directories, or <see langword="null"/>.</param>
		public ChatEnvironmentSettingsViewModel(ChatEnvironmentSettings settings,
			IEnumerable<IScriptEngineEnvConfigurationProvider> scriptEngineConfigProviders, IExplorerOpener? explorerOpener)
		{
			_scriptEngineConfigProviders = scriptEngineConfigProviders.ToList();
			_explorerOpener = explorerOpener;

			EnvironmentSettings = settings;

			_selectedWorkingDirectoriesInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == settings.WorkingDirectoriesInheritance);
			_selectedDirectoryAccessRulesInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == settings.DirectoryAccessRulesInheritance);
			_selectedAdditionalSettingsInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == settings.AdditionalSettingsInheritance);

			settings.PropertyChanged += EnvironmentSettings_PropertyChanged;

			RebuildAdditionalEnvironmentSettings();

			AddWorkingDirectoryCommand = new RelayCommand(AddWorkingDirectory);
			RemoveWorkingDirectoryCommand = new RelayCommand<WorkingDirectorySetting>(RemoveWorkingDirectory);
			MoveWorkingDirectoryUpCommand = new RelayCommand<WorkingDirectorySetting>(MoveWorkingDirectoryUp);
			MoveWorkingDirectoryDownCommand = new RelayCommand<WorkingDirectorySetting>(MoveWorkingDirectoryDown);
			BrowseWorkingDirectoryPathCommand = new AsyncRelayCommand<WorkingDirectorySetting>(BrowseWorkingDirectoryPath);
			SetDefaultWorkingDirectoryActiveCommand = new RelayCommand(SetDefaultWorkingDirectoryActive);
			SetActiveWorkingDirectoryCommand = new RelayCommand<WorkingDirectorySetting>(SetActiveWorkingDirectory);

			AddDirectoryAccessRuleCommand = new RelayCommand(AddDirectoryAccessRule);
			RemoveDirectoryAccessRuleCommand = new RelayCommand<DirectoryAccessSetting>(RemoveDirectoryAccessRule);
			MoveDirectoryAccessRuleUpCommand = new RelayCommand<DirectoryAccessSetting>(MoveDirectoryAccessRuleUp);
			MoveDirectoryAccessRuleDownCommand = new RelayCommand<DirectoryAccessSetting>(MoveDirectoryAccessRuleDown);
			BrowseDirectoryAccessRulePathCommand = new AsyncRelayCommand<DirectoryAccessSetting>(BrowseDirectoryAccessRulePath);

			OpenDirectoryCommand = new RelayCommand<string?>(OpenDirectory);
		}

		private void EnvironmentSettings_PropertyChanged(object? sender, PropertyChangedEventArgs e)
		{
			switch (e.PropertyName)
			{
				case nameof(ChatEnvironmentSettings.WorkingDirectoriesInheritance):
					_selectedWorkingDirectoriesInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == EnvironmentSettings.WorkingDirectoriesInheritance);
					RaisePropertyChanged(nameof(SelectedWorkingDirectoriesInheritance));
					RaisePropertyChanged(nameof(EffectiveWorkingDirectories));
					break;

				case nameof(ChatEnvironmentSettings.DirectoryAccessRulesInheritance):
					_selectedDirectoryAccessRulesInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == EnvironmentSettings.DirectoryAccessRulesInheritance);
					RaisePropertyChanged(nameof(SelectedDirectoryAccessRulesInheritance));
					RaisePropertyChanged(nameof(EffectiveDirectoryAccessRules));
					break;

				case nameof(ChatEnvironmentSettings.AdditionalSettingsInheritance):
					_selectedAdditionalSettingsInheritance = InheritanceLevelItem.AllProfile.First(i => i.Value == EnvironmentSettings.AdditionalSettingsInheritance);
					RaisePropertyChanged(nameof(SelectedAdditionalSettingsInheritance));
					RaisePropertyChanged(nameof(EffectiveAdditionalSettings));
					RebuildAdditionalEnvironmentSettings();
					break;
			}
		}

		private void RebuildAdditionalEnvironmentSettings()
		{
			var builder = ImmutableList.CreateBuilder<ScriptEnvironmentSettingsItemViewModel>();
			var effectiveAdditionalSettings = EnvironmentSettings.GetEffectiveAdditionalSettings();

			foreach (var provider in _scriptEngineConfigProviders)
			{
				var foundConfig = provider.FindConfiguration(effectiveAdditionalSettings);
				if (foundConfig is null)
				{
					foundConfig = provider.CreateConfiguration();
					effectiveAdditionalSettings.Add(foundConfig);
				}
				var viewModel = provider.CreateViewModel(foundConfig);
				builder.Add(new ScriptEnvironmentSettingsItemViewModel(provider, EnvironmentSettings, foundConfig, viewModel));
			}

			AdditionalEnvironmentSettings = builder.ToImmutable();
		}

		private void SetDefaultWorkingDirectoryActive()
		{
			var workingDirectories = EnvironmentSettings.GetEffectiveWorkingDirectories();
			workingDirectories.IsDefaultWorkingDirectoryActive = true;
			foreach (var wd in workingDirectories.Items)
				wd.IsActive = false;
		}

		private void SetActiveWorkingDirectory(WorkingDirectorySetting? wd)
		{
			if (wd == null)
				return;
			var workingDirectories = EnvironmentSettings.GetEffectiveWorkingDirectories();
			workingDirectories.IsDefaultWorkingDirectoryActive = false;
			foreach (var item in workingDirectories.Items)
				item.IsActive = item == wd;
		}

		private void AddWorkingDirectory()
		{
			var items = EnvironmentSettings.GetEffectiveWorkingDirectories().Items;
			var wd = new WorkingDirectorySetting
			{
				Name = "New working directory",
				Path = string.Empty,
				IsEnabled = true,
				IsActive = !items.Any(w => w.IsActive && w.IsEnabled)
			};
			items.Add(wd);
		}

		private void RemoveWorkingDirectory(WorkingDirectorySetting? wd)
		{
			if (wd == null)
				return;
			EnvironmentSettings.GetEffectiveWorkingDirectories().Items.Remove(wd);
		}

		private void MoveWorkingDirectoryUp(WorkingDirectorySetting? wd)
		{
			if (wd == null)
				return;
			var items = EnvironmentSettings.GetEffectiveWorkingDirectories().Items;
			var index = items.IndexOf(wd);
			if (index > 0)
				items.Move(index, index - 1);
		}

		private void MoveWorkingDirectoryDown(WorkingDirectorySetting? wd)
		{
			if (wd == null)
				return;
			var items = EnvironmentSettings.GetEffectiveWorkingDirectories().Items;
			var index = items.IndexOf(wd);
			if (index < items.Count - 1)
				items.Move(index, index + 1);
		}

		private async Task BrowseWorkingDirectoryPath(WorkingDirectorySetting? wd)
		{
			if (wd == null)
				return;

			var result = await App.MainTopLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
			{
				Title = LocalizationManager.LocalizeStatic("env.select_working_directory"),
				AllowMultiple = false
			});

			if (result.Count > 0)
			{
				wd.Path = result[0].Path.LocalPath;
			}
		}

		private void AddDirectoryAccessRule()
		{
			var rule = new DirectoryAccessSetting
			{
				Path = string.Empty,
				AccessMode = DirectoryAccessMode.Read
			};
			EnvironmentSettings.GetEffectiveDirectoryAccessRules().Add(rule);
		}

		private void MoveDirectoryAccessRuleUp(DirectoryAccessSetting? rule)
		{
			if (rule == null)
				return;
			var rules = EnvironmentSettings.GetEffectiveDirectoryAccessRules();
			var index = rules.IndexOf(rule);
			if (index > 0)
				rules.Move(index, index - 1);
		}

		private void MoveDirectoryAccessRuleDown(DirectoryAccessSetting? rule)
		{
			if (rule == null)
				return;
			var rules = EnvironmentSettings.GetEffectiveDirectoryAccessRules();
			var index = rules.IndexOf(rule);
			if (index < rules.Count - 1)
				rules.Move(index, index + 1);
		}

		private void RemoveDirectoryAccessRule(DirectoryAccessSetting? rule)
		{
			if (rule == null)
				return;
			EnvironmentSettings.GetEffectiveDirectoryAccessRules().Remove(rule);
		}

		private async Task BrowseDirectoryAccessRulePath(DirectoryAccessSetting? rule)
		{
			if (rule == null)
				return;

			var result = await App.MainTopLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
			{
				Title = LocalizationManager.LocalizeStatic("env.select_working_directory"),
				AllowMultiple = false
			});

			if (result.Count > 0)
			{
				rule.Path = result[0].Path.LocalPath;
			}
		}

		private void OpenDirectory(string? path)
		{
			if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
			{
				_explorerOpener?.OpenDirectory(path);
			}
		}

		/// <inheritdoc/>
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
				EnvironmentSettings.PropertyChanged -= EnvironmentSettings_PropertyChanged;
		}
	}
}
