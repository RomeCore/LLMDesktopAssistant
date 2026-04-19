using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Localization.Resources;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace LLMDesktopAssistant.LLM.Settings
{
	[ViewModelFor(typeof(ChatGeneralSettingsView))]
	public class ChatGeneralSettingsViewModel : ViewModelBase
	{
		public ChatSettingsViewModel Parent { get; }
		public ChatSettings Settings { get; }

		public ICommand SelectWorkingDirectoryCommand { get; }
		public ICommand OpenWorkingDirectoryCommand { get; }
		public ICommand SelectPythonVenvActivateScriptPathCommand { get; }
		public ICommand OpenPythonVenvActivateScriptPathCommand { get; }

		public ChatGeneralSettingsViewModel(ChatSettingsViewModel parent)
		{
			Parent = parent;
			Settings = Parent.Settings;

			SelectWorkingDirectoryCommand = new AsyncRelayCommand(SelectWorkingDirectory);
			OpenWorkingDirectoryCommand = new RelayCommand(OpenWorkingDirectory);
			SelectPythonVenvActivateScriptPathCommand = new AsyncRelayCommand(SelectPythonVenvActivateScriptPath);
			OpenPythonVenvActivateScriptPathCommand = new RelayCommand(OpenPythonVenvActivateScriptPath);
		}

		private async Task SelectWorkingDirectory()
		{
			var result = await App.MainTopLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
			{
				Title = Locale.select_working_directory,
				AllowMultiple = false
			});

			if (result.Count > 0)
			{
				Settings.WorkingDirectory = result[0].Path.LocalPath;
			}
		}

		private void OpenWorkingDirectory()
		{
			if (!string.IsNullOrWhiteSpace(Settings.WorkingDirectory) &&
				Directory.Exists(Settings.WorkingDirectory))
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = Settings.WorkingDirectory,
					UseShellExecute = true
				});
			}
		}

		private async Task SelectPythonVenvActivateScriptPath()
		{
			var result = await App.MainTopLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
			{
				Title = Locale.select_python_venv_activate_script,
				FileTypeFilter = [
					new FilePickerFileType("Batch files") { Patterns = ["*.bat"] },
					new FilePickerFileType("All files") { Patterns = ["*"] }
				],
				AllowMultiple = false
			});

			if (result.Count > 0)
			{
				Settings.PythonVenvActivateScriptPath = result[0].Path.LocalPath;
			}
		}

		private void OpenPythonVenvActivateScriptPath()
		{
			if (!string.IsNullOrWhiteSpace(Settings.PythonVenvActivateScriptPath) &&
				File.Exists(Settings.PythonVenvActivateScriptPath))
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "explorer.exe",
					Arguments = $"/select,\"{Settings.PythonVenvActivateScriptPath}\"",
					UseShellExecute = true
				});
			}
		}
	}
}