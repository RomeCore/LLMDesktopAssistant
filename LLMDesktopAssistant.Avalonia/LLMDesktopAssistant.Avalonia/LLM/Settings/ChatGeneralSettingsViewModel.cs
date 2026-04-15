using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Core.LLM.Domain;
using LLMDesktopAssistant.Core.Localization.Resources;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace LLMDesktopAssistant.Avalonia.LLM.Settings
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
				Settings.WorkingDirectory = result[0].Path.AbsolutePath;
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
				Title = Locale.select_working_directory,
				FileTypeFilter = [
					new FilePickerFileType("Batch files (*.bat)"),
					new FilePickerFileType("All files (*)")
				],
				AllowMultiple = false
			});

			if (result.Count > 0)
			{
				Settings.PythonVenvActivateScriptPath = result[0].Path.AbsolutePath;
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