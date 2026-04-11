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

		public ChatGeneralSettingsViewModel(ChatSettingsViewModel parent)
		{
			Parent = parent;
			Settings = Parent.Settings;

			SelectWorkingDirectoryCommand = new RelayCommand(SelectWorkingDirectory);
			OpenWorkingDirectoryCommand = new RelayCommand(OpenWorkingDirectory);
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
	}
}