using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Core.LLM.Domain;
using LLMDesktopAssistant.Core.Localization.Resources;
using LLMDesktopAssistant.Core.MVVM;
using System.Diagnostics;
using System.IO;
using System.Windows.Input;

namespace LLMDesktopAssistant.Core.LLM.MVVM.Settings
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

			SelectWorkingDirectoryCommand = new RelayCommand(SelectWorkingDirectory);
			OpenWorkingDirectoryCommand = new RelayCommand(OpenWorkingDirectory);
			SelectPythonVenvActivateScriptPathCommand = new RelayCommand(SelectPythonVenvActivateScriptPath);
			OpenPythonVenvActivateScriptPathCommand = new RelayCommand(OpenPythonVenvActivateScriptPath);
		}

		private void SelectWorkingDirectory()
		{
			var dialog = new System.Windows.Forms.FolderBrowserDialog
			{
				Description = Locale.select_working_directory,
				UseDescriptionForTitle = true,
				ShowNewFolderButton = true
			};

			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				Settings.WorkingDirectory = dialog.SelectedPath;
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

		private void SelectPythonVenvActivateScriptPath()
		{
			var dialog = new System.Windows.Forms.OpenFileDialog
			{
				Title = Locale.select_python_venv_activate_script,
				Filter = "Batch files (*.bat)|*.bat|All files (*)|*",
				Multiselect = false
			};

			if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				Settings.PythonVenvActivateScriptPath = dialog.FileName;
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