using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using LiteDB;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Localization;

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

	private string? _selectedPath;
	public string? SelectedPath
	{
		get => _selectedPath;
		set
		{
			if (SetProperty(ref _selectedPath, value))
				RaisePropertyChanged(nameof(CanSubmit));
		}
	}

	public List<string>? SelectedPaths { get; private set; }

	public bool CanSubmit =>
		(AllowMultiple ? SelectedPaths?.Count > 0 : !string.IsNullOrWhiteSpace(SelectedPath)) == true;

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

	public void SetPaths(string[] paths)
	{
		if (IsResultSet) return;

		if (AllowMultiple)
		{
			SelectedPaths = [.. paths];
			SelectedPath = null;
		}
		else
		{
			SelectedPath = paths.FirstOrDefault();
			SelectedPaths = null;
		}

		RaisePropertyChanged(nameof(CanSubmit));
		SubmitCommand.NotifyCanExecuteChanged();
	}

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
				SetPaths(resultDirs.Select(r => r.Path.LocalPath).ToArray());

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
				SetPaths(resultFiles.Select(r => r.Path.LocalPath).ToArray());

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
					SetPaths([resultFile.Path.LocalPath]);
				else
					SetPaths([]);

				break;
		}
	}

	public void Submit()
	{
		if (IsResultSet || !CanSubmit) return;
		IsResultSet = true;

		_tcs.TrySetResult(new FilePickerResult
		{
			Paths = AllowMultiple
				? (SelectedPaths ?? []).ToArray()
				: [SelectedPath ?? string.Empty],
			SinglePath = AllowMultiple ? null : SelectedPath
		});
	}

	public FormsFilePickerViewModel()
	{
		PickCommand = new AsyncRelayCommand(PickFiles, () => !IsResultSet);
		SubmitCommand = new RelayCommand(Submit, () => CanSubmit && !IsResultSet);
	}
}

public class FilePickerResult
{
	public required string[] Paths { get; init; }

	public string? SinglePath { get; init; }
}
