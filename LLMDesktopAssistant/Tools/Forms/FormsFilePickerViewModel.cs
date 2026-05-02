using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Tools.Forms;

public enum FilePickerMode
{
	Directory,
	Open,
	Save
}

[ViewModelFor(typeof(FormsFilePickerView))]
public class FormsFilePickerViewModel : AdditionalMessageViewModel
{
	private readonly TaskCompletionSource<FilePickerResult> _tcs = new();

	[BsonIgnore]
	public Task<FilePickerResult> Result => _tcs.Task;

	private string _title = string.Empty;
	public string Title
	{
		get => _title;
		set => SetProperty(ref _title, value);
	}

	private string _description = string.Empty;
	public string Description
	{
		get => _description;
		set => SetProperty(ref _description, value);
	}

	private FilePickerMode _mode;
	public FilePickerMode Mode
	{
		get => _mode;
		set => SetProperty(ref _mode, value);
	}

	private string? _filter;
	/// <summary>
	/// Extension filter, e.g. '*.cs;*.py;*.js'
	/// </summary>
	public string? Filter
	{
		get => _filter;
		set => SetProperty(ref _filter, value);
	}

	private bool _allowMultiple;
	public bool AllowMultiple
	{
		get => _allowMultiple;
		set => SetProperty(ref _allowMultiple, value);
	}

	private RangeObservableCollection<string> _selectedPaths = [];
	public ICollection<string> SelectedPaths
	{
		get => _selectedPaths;
		set => _selectedPaths.Reset(value);
	}

	public bool CanSubmit => SelectedPaths.Count > 0;

	private bool _isResultSet;
	public bool IsResultSet
	{
		get => _isResultSet;
		private set => SetProperty(ref _isResultSet, value);
	}

	[BsonIgnore]
	public IRelayCommand PickCommand { get; }

	[BsonIgnore]
	public IRelayCommand SubmitCommand { get; }

	private async Task PickFiles()
	{
		var storageProvider = App.MainTopLevel.StorageProvider;

		switch (Mode)
		{
			case FilePickerMode.Directory:

				var dirParams = new FolderPickerOpenOptions
				{
					AllowMultiple = AllowMultiple,
					Title = Title
				};

				var resultDirs = await storageProvider.OpenFolderPickerAsync(dirParams);
				SelectedPaths = resultDirs.Select(r => r.Path.LocalPath).ToArray();

				break;

			case FilePickerMode.Open:

				var fileParams = new FilePickerOpenOptions
				{
					AllowMultiple = AllowMultiple,
					Title = Title,
					FileTypeFilter = [new FilePickerFileType(LocalizationManager.LocalizeStatic("forms_selected_files"))
					{
						Patterns = Filter?.Split(';')
					}]
				};

				var resultFiles = await storageProvider.OpenFilePickerAsync(fileParams);
				SelectedPaths = resultFiles.Select(r => r.Path.LocalPath).ToArray();

				break;

			case FilePickerMode.Save:

				var saveParams = new FilePickerSaveOptions
				{
					Title = Title,
					FileTypeChoices = [new FilePickerFileType(LocalizationManager.LocalizeStatic("forms_selected_files"))
					{
						Patterns = Filter?.Split(';')
					}]
				};

				var resultFile = await storageProvider.SaveFilePickerAsync(saveParams);
				if (resultFile != null)
					SelectedPaths = [resultFile.Path.LocalPath];
				else
					SelectedPaths = [];

				break;
		}
	}

	public void Submit()
	{
		if (IsResultSet || !CanSubmit) return;
		IsResultSet = true;

		_tcs.TrySetResult(new FilePickerResult
		{
			Paths = SelectedPaths.ToArray()
		});
	}

	public FormsFilePickerViewModel()
	{
		PickCommand = new AsyncRelayCommand(PickFiles, () => !IsResultSet);
		SubmitCommand = new RelayCommand(Submit, () => CanSubmit && !IsResultSet);
		_selectedPaths.CollectionChanged += (_, _) =>
		{
			RaisePropertyChanged(nameof(CanSubmit));
			SubmitCommand.NotifyCanExecuteChanged();
		};
	}
}

public class FilePickerResult
{
	public required string[] Paths { get; init; }
}
