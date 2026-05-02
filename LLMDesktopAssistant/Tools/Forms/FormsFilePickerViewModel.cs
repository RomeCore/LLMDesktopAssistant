using LLMDesktopAssistant.LLM.Domain;


using CommunityToolkit.Mvvm.Input;

namespace LLMDesktopAssistant.Tools.Forms;

/// <summary>
/// ViewModel для выбора файла через диалог.
/// </summary>
[ViewModelFor(typeof(FormsFilePickerView))]
public class FormsFilePickerViewModel : AdditionalMessageViewModel
{
    private readonly TaskCompletionSource<FilePickerResult> _tcs = new();

    /// <summary>
    /// Таска с результатом выбора файла.
    /// </summary>
    public Task<FilePickerResult> Result => _tcs.Task;

    private string _title = string.Empty;
    /// <summary>
    /// Заголовок диалога.
    /// </summary>
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

    private string? _filter;
    /// <summary>
    /// Фильтр файлов (например "*.cs;*.py;*.js").
    /// </summary>
    public string? Filter
    {
        get => _filter;
        set => SetProperty(ref _filter, value);
    }

    private bool _allowMultiple;
    /// <summary>
    /// Можно ли выбрать несколько файлов.
    /// </summary>
    public bool AllowMultiple
    {
        get => _allowMultiple;
        set => SetProperty(ref _allowMultiple, value);
    }

    private string? _selectedPath;
    /// <summary>
    /// Выбранный путь (если single).
    /// </summary>
    public string? SelectedPath
    {
        get => _selectedPath;
        set
        {
            if (SetProperty(ref _selectedPath, value))
                RaisePropertyChanged(nameof(CanSubmit));
        }
    }

    /// <summary>
    /// Выбранные пути (если multiple).
    /// </summary>
    public List<string>? SelectedPaths { get; private set; }

    /// <summary>
    /// Можно ли подтвердить выбор.
    /// </summary>
    public bool CanSubmit =>
        (AllowMultiple ? SelectedPaths?.Count > 0 : !string.IsNullOrWhiteSpace(SelectedPath)) == true;

    private bool _isResultSet;
    public bool IsResultSet
    {
        get => _isResultSet;
        private set => SetProperty(ref _isResultSet, value);
    }

    /// <summary>
    /// Команда открытия диалога выбора файла.
    /// </summary>
    public ICommand PickCommand => new RelayCommand(PickFile, () => !IsResultSet);

    /// <summary>
    /// Команда подтверждения выбора.
    /// </summary>
    public ICommand SubmitCommand => new RelayCommand(Submit, () => CanSubmit && !IsResultSet);

    /// <summary>
    /// Событие для запроса открытия диалога выбора файла.
    /// View подписывается и открывает файловый диалог Avalonia.
    /// </summary>
    public event EventHandler? FilePickRequested;

    /// <summary>
    /// Открыть диалог выбора файла.
    /// </summary>
    public void PickFile()
    {
        FilePickRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Устанавливает результат выбора файла из диалога.
    /// </summary>
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
    }

    /// <summary>
    /// Подтвердить выбор.
    /// </summary>
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
        IsTemporary = true;
    }
}

/// <summary>
/// Результат выбора файла.
/// </summary>
public class FilePickerResult
{
    /// <summary>
    /// Выбранные пути.
    /// </summary>
    public required string[] Paths { get; init; }

    /// <summary>
    /// Одиночный путь (если AllowMultiple = false).
    /// </summary>
    public string? SinglePath { get; init; }
}
