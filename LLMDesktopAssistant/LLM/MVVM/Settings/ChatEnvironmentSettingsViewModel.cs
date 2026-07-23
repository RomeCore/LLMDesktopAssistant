using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Localization.Resources;
using System.Diagnostics;
using System.IO;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// ViewModel for the Environment settings tab.
	/// Manages working directories, directory access rules, and Python environment paths.
	/// </summary>
	[ViewModelFor(typeof(ChatEnvironmentSettingsView))]
	public class ChatEnvironmentSettingsViewModel : ViewModelBase
	{
		public ChatEnvironmentSettings EnvironmentSettings { get; }

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

		// Python venv commands (unchanged)
		public IRelayCommand SelectPythonVenvActivateScriptPathCommand { get; }
		public IRelayCommand OpenPythonVenvActivateScriptPathCommand { get; }
		public IRelayCommand SelectPythonMetaVenvActivateScriptPathCommand { get; }
		public IRelayCommand OpenPythonMetaVenvActivateScriptPathCommand { get; }

		public ChatEnvironmentSettingsViewModel(ChatEnvironmentSettings settings)
		{
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

			SelectPythonVenvActivateScriptPathCommand = new AsyncRelayCommand(SelectPythonVenvActivateScriptPath);
			OpenPythonVenvActivateScriptPathCommand = new RelayCommand(OpenPythonVenvActivateScriptPath);
			SelectPythonMetaVenvActivateScriptPathCommand = new AsyncRelayCommand(SelectPythonMetaVenvActivateScriptPath);
			OpenPythonMetaVenvActivateScriptPathCommand = new RelayCommand(OpenPythonMetaVenvActivateScriptPath);
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

		private async Task SelectPythonVenvActivateScriptPath()
		{
			var result = await App.MainTopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = LocalizationManager.LocalizeStatic("select_python_venv_activate_script"),
				FileTypeFilter = [
					new FilePickerFileType("Batch files") { Patterns = ["*.bat"] },
					new FilePickerFileType("All files") { Patterns = ["*"] }
				],
				AllowMultiple = false
			});

			if (result.Count > 0)
			{
				EnvironmentSettings.PythonVenvActivateScriptPath = result[0].Path.LocalPath;
			}
		}

		private void OpenPythonVenvActivateScriptPath()
		{
			if (!string.IsNullOrWhiteSpace(EnvironmentSettings.PythonVenvActivateScriptPath) &&
				File.Exists(EnvironmentSettings.PythonVenvActivateScriptPath))
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "explorer.exe",
					Arguments = $"/select,\"{EnvironmentSettings.PythonVenvActivateScriptPath}\"",
					UseShellExecute = true
				});
			}
		}

		private async Task SelectPythonMetaVenvActivateScriptPath()
		{
			var result = await App.MainTopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = LocalizationManager.LocalizeStatic("select_python_meta_venv_activate_script"),
				FileTypeFilter = [
					new FilePickerFileType("Batch files") { Patterns = ["*.bat"] },
					new FilePickerFileType("All files") { Patterns = ["*"] }
				],
				AllowMultiple = false
			});

			if (result.Count > 0)
			{
				EnvironmentSettings.PythonMetaVenvActivateScriptPath = result[0].Path.LocalPath;
			}
		}

		private void OpenPythonMetaVenvActivateScriptPath()
		{
			if (!string.IsNullOrWhiteSpace(EnvironmentSettings.PythonMetaVenvActivateScriptPath) &&
				File.Exists(EnvironmentSettings.PythonMetaVenvActivateScriptPath))
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "explorer.exe",
					Arguments = $"/select,\"{EnvironmentSettings.PythonMetaVenvActivateScriptPath}\"",
					UseShellExecute = true
				});
			}
		}

		private void OpenDirectory(string? path)
		{
			if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = path,
					UseShellExecute = true
				});
			}
		}
	}
}
