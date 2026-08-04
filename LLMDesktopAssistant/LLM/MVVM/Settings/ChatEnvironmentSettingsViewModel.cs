using Avalonia.Platform.Storage;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Utils;
using System.ComponentModel;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// ViewModel for the Environment settings tab.
	/// Manages working directories, directory access rules, and Python environment paths.
	/// </summary>
	[ViewModelFor(typeof(ChatEnvironmentSettingsView))]
	public class ChatEnvironmentSettingsViewModel : ViewModelBase
	{
		private List<IScriptEngineEnvConfigurationProvider> _scriptEngineConfigProviders;
		private readonly IExplorerOpener? _explorerOpener;

		public ChatEnvironmentSettings EnvironmentSettings { get; }

		public ImmutableList<ScriptEnvironmentSettingsItemViewModel> AdditionalEnvironmentSettings { get; }

		public IRelayCommand AddWorkingDirectoryCommand { get; }
		public IRelayCommand<WorkingDirectorySetting> RemoveWorkingDirectoryCommand { get; }
		public IRelayCommand<WorkingDirectorySetting> MoveWorkingDirectoryUpCommand { get; }
		public IRelayCommand<WorkingDirectorySetting> MoveWorkingDirectoryDownCommand { get; }
		public IRelayCommand<WorkingDirectorySetting> BrowseWorkingDirectoryPathCommand { get; }

		public IRelayCommand AddDirectoryAccessRuleCommand { get; }
		public IRelayCommand<DirectoryAccessSetting> RemoveDirectoryAccessRuleCommand { get; }
		public IRelayCommand<DirectoryAccessSetting> MoveDirectoryAccessRuleUpCommand { get; }
		public IRelayCommand<DirectoryAccessSetting> MoveDirectoryAccessRuleDownCommand { get; }
		public IRelayCommand<DirectoryAccessSetting> BrowseDirectoryAccessRulePathCommand { get; }

		public IRelayCommand<string?> OpenDirectoryCommand { get; }

		public ChatEnvironmentSettingsViewModel(ChatEnvironmentSettings settings,
			IEnumerable<IScriptEngineEnvConfigurationProvider> scriptEngineConfigProviders, IExplorerOpener? explorerOpener)
		{
			_scriptEngineConfigProviders = scriptEngineConfigProviders.ToList();
			_explorerOpener = explorerOpener;

			var additionalEnvBuilder = ImmutableList.CreateBuilder<ScriptEnvironmentSettingsItemViewModel>();

			foreach (var provider in _scriptEngineConfigProviders)
			{
				var foundConfig = provider.FindConfiguration(settings.AdditionalSettings);
				if (foundConfig is null)
				{
					foundConfig = provider.CreateConfiguration();
					settings.AdditionalSettings.Add(foundConfig);
				}
				var viewModel = provider.CreateViewModel(foundConfig);
				additionalEnvBuilder.Add(new ScriptEnvironmentSettingsItemViewModel(provider, settings, foundConfig, viewModel));
			}

			AdditionalEnvironmentSettings = additionalEnvBuilder.ToImmutable();

			EnvironmentSettings = settings;

			AddWorkingDirectoryCommand = new RelayCommand(AddWorkingDirectory);
			RemoveWorkingDirectoryCommand = new RelayCommand<WorkingDirectorySetting>(RemoveWorkingDirectory);
			MoveWorkingDirectoryUpCommand = new RelayCommand<WorkingDirectorySetting>(MoveWorkingDirectoryUp);
			MoveWorkingDirectoryDownCommand = new RelayCommand<WorkingDirectorySetting>(MoveWorkingDirectoryDown);
			BrowseWorkingDirectoryPathCommand = new AsyncRelayCommand<WorkingDirectorySetting>(BrowseWorkingDirectoryPath);

			AddDirectoryAccessRuleCommand = new RelayCommand(AddDirectoryAccessRule);
			RemoveDirectoryAccessRuleCommand = new RelayCommand<DirectoryAccessSetting>(RemoveDirectoryAccessRule);
			MoveDirectoryAccessRuleUpCommand = new RelayCommand<DirectoryAccessSetting>(MoveDirectoryAccessRuleUp);
			MoveDirectoryAccessRuleDownCommand = new RelayCommand<DirectoryAccessSetting>(MoveDirectoryAccessRuleDown);
			BrowseDirectoryAccessRulePathCommand = new AsyncRelayCommand<DirectoryAccessSetting>(BrowseDirectoryAccessRulePath);

			OpenDirectoryCommand = new RelayCommand<string?>(OpenDirectory);
		}

		private void AddWorkingDirectory()
		{
			var wd = new WorkingDirectorySetting
			{
				Name = "New working directory",
				Path = string.Empty,
				IsEnabled = true,
				IsActive = !EnvironmentSettings.WorkingDirectories.Any(w => w.IsActive && w.IsEnabled)
			};
			EnvironmentSettings.WorkingDirectories.Add(wd);
		}

		private void RemoveWorkingDirectory(WorkingDirectorySetting? wd)
		{
			if (wd == null)
				return;
			EnvironmentSettings.WorkingDirectories.Remove(wd);
		}

		private void MoveWorkingDirectoryUp(WorkingDirectorySetting? wd)
		{
			if (wd == null)
				return;
			var index = EnvironmentSettings.WorkingDirectories.IndexOf(wd);
			if (index > 0)
				EnvironmentSettings.WorkingDirectories.Move(index, index - 1);
		}

		private void MoveWorkingDirectoryDown(WorkingDirectorySetting? wd)
		{
			if (wd == null)
				return;
			var index = EnvironmentSettings.WorkingDirectories.IndexOf(wd);
			if (index < EnvironmentSettings.WorkingDirectories.Count - 1)
				EnvironmentSettings.WorkingDirectories.Move(index, index + 1);
		}

		private async Task BrowseWorkingDirectoryPath(WorkingDirectorySetting? wd)
		{
			if (wd == null)
				return;

			var result = await App.MainTopLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
			{
				Title = LocalizationManager.LocalizeStatic("select_working_directory"),
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
			EnvironmentSettings.DirectoryAccessRules.Add(rule);
		}

		private void MoveDirectoryAccessRuleUp(DirectoryAccessSetting? rule)
		{
			if (rule == null)
				return;
			var index = EnvironmentSettings.DirectoryAccessRules.IndexOf(rule);
			if (index > 0)
				EnvironmentSettings.DirectoryAccessRules.Move(index, index - 1);
		}

		private void MoveDirectoryAccessRuleDown(DirectoryAccessSetting? rule)
		{
			if (rule == null)
				return;
			var index = EnvironmentSettings.DirectoryAccessRules.IndexOf(rule);
			if (index < EnvironmentSettings.DirectoryAccessRules.Count - 1)
				EnvironmentSettings.DirectoryAccessRules.Move(index, index + 1);
		}

		private void RemoveDirectoryAccessRule(DirectoryAccessSetting? rule)
		{
			if (rule == null)
				return;
			EnvironmentSettings.DirectoryAccessRules.Remove(rule);
		}

		private async Task BrowseDirectoryAccessRulePath(DirectoryAccessSetting? rule)
		{
			if (rule == null)
				return;

			var result = await App.MainTopLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
			{
				Title = LocalizationManager.LocalizeStatic("select_working_directory"),
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
	}
}
