using System.IO;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services.Instances;

namespace LLMDesktopAssistant.Desktop.Scripting.Python
{
	[ViewModelFor(typeof(PythonEnvironmentConfigurationView))]
	public class PythonEnvironmentConfigurationViewModel : NotifyPropertyChanged
	{
		private readonly IExplorerOpener _explorerOpener;

		public PythonEnvironmentConfiguration Configuration { get; }

		public IRelayCommand SelectPythonVenvActivateScriptPathCommand { get; }
		public IRelayCommand OpenPythonVenvActivateScriptPathCommand { get; }
		public IRelayCommand SelectPythonMetaVenvActivateScriptPathCommand { get; }
		public IRelayCommand OpenPythonMetaVenvActivateScriptPathCommand { get; }

		public PythonEnvironmentConfigurationViewModel(PythonEnvironmentConfiguration configuration, IExplorerOpener explorerOpener)
		{
			_explorerOpener = explorerOpener;
			Configuration = configuration;

			SelectPythonVenvActivateScriptPathCommand = new AsyncRelayCommand(SelectPythonVenvActivateScriptPath);
			OpenPythonVenvActivateScriptPathCommand = new RelayCommand(OpenPythonVenvActivateScriptPath);
			SelectPythonMetaVenvActivateScriptPathCommand = new AsyncRelayCommand(SelectPythonMetaVenvActivateScriptPath);
			OpenPythonMetaVenvActivateScriptPathCommand = new RelayCommand(OpenPythonMetaVenvActivateScriptPath);
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
				Configuration.PythonVenvActivateScriptPath = result[0].Path.LocalPath;
			}
		}

		private void OpenPythonVenvActivateScriptPath()
		{
			if (!string.IsNullOrWhiteSpace(Configuration.PythonVenvActivateScriptPath) &&
				File.Exists(Configuration.PythonVenvActivateScriptPath))
			{
				_explorerOpener?.ShowFileInExplorer(Configuration.PythonVenvActivateScriptPath);
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
				Configuration.PythonMetaVenvActivateScriptPath = result[0].Path.LocalPath;
			}
		}

		private void OpenPythonMetaVenvActivateScriptPath()
		{
			if (!string.IsNullOrWhiteSpace(Configuration.PythonMetaVenvActivateScriptPath) &&
				File.Exists(Configuration.PythonMetaVenvActivateScriptPath))
			{
				_explorerOpener?.ShowFileInExplorer(Configuration.PythonMetaVenvActivateScriptPath);
			}
		}
	}
}
