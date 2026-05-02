using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.Tools.Forms;

/// <summary>
/// ViewModel для формы подтверждения. Показывает вопрос и две кнопки.
/// </summary>
[ViewModelFor(typeof(FormsConfirmView))]
public class FormsConfirmViewModel : AdditionalMessageViewModel
{
    private readonly TaskCompletionSource<bool> _tcs = new();

    /// <summary>
    /// Таска, которая завершится, когда юзер нажмёт кнопку.
    /// true — подтвердил, false — отказал.
    /// </summary>
    public Task<bool> Result => _tcs.Task;

    private string _title = string.Empty;
    /// <summary>
    /// Заголовок вопроса.
    /// </summary>
    public string Title
    {
        get => _title;
        set => SetProperty(ref _title, value);
    }

    private string _description = string.Empty;
    /// <summary>
    /// Описание или дополнительный контекст.
    /// </summary>
    public string Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    private string _confirmText = "OK";
    /// <summary>
    /// Текст на кнопке подтверждения.
    /// </summary>
    public string ConfirmText
    {
        get => _confirmText;
        set => SetProperty(ref _confirmText, value);
    }

    private string _cancelText = "Отмена";
    /// <summary>
    /// Текст на кнопке отмены.
    /// </summary>
    public string CancelText
    {
        get => _cancelText;
        set => SetProperty(ref _cancelText, value);
    }

    private bool _isDanger;
    /// <summary>
    /// Опасное действие? Кнопка будет красной.
    /// </summary>
    public bool IsDanger
    {
        get => _isDanger;
        set => SetProperty(ref _isDanger, value);
    }

    private bool _isResultSet;
    /// <summary>
    /// Блокируем повторное нажатие после выбора.
    /// </summary>
    public bool IsResultSet
    {
        get => _isResultSet;
        private set => SetProperty(ref _isResultSet, value);
    }

    /// <summary>
    /// Команда подтверждения.
    /// </summary>
    public ICommand ConfirmCommand => new RelayCommand(Confirm, () => !IsResultSet);

    /// <summary>
    /// Команда отмены.
    /// </summary>
    public ICommand CancelCommand => new RelayCommand(Cancel, () => !IsResultSet);

    /// <summary>
    /// Вызывается при нажатии кнопки подтверждения.
    /// </summary>
    public void Confirm()
    {
        if (IsResultSet) return;
        IsResultSet = true;
        _tcs.TrySetResult(true);
    }

    /// <summary>
    /// Вызывается при нажатии кнопки отмены.
    /// </summary>
    public void Cancel()
    {
        if (IsResultSet) return;
        IsResultSet = true;
        _tcs.TrySetResult(false);
    }

    public FormsConfirmViewModel()
    {
        IsTemporary = true;
    }
}
