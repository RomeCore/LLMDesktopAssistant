using CommunityToolkit.Mvvm.Input;
using LiteDB;
using LLMDesktopAssistant.LLM.Domain;

namespace LLMDesktopAssistant.Tools.Forms;

[ViewModelFor(typeof(FormsConfirmView))]
public class FormsConfirmViewModel : AdditionalMessageViewModel
{
	private readonly TaskCompletionSource<bool> _tcs = new();

	[BsonIgnore]
	public Task<bool> Result => _tcs.Task;

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

	private string _confirmText = "OK";
	public string ConfirmText
	{
		get => _confirmText;
		set => SetProperty(ref _confirmText, value);
	}

	private string _cancelText = "Cancel";
	public string CancelText
	{
		get => _cancelText;
		set => SetProperty(ref _cancelText, value);
	}

	private bool _isDanger;
	public bool IsDanger
	{
		get => _isDanger;
		set => SetProperty(ref _isDanger, value);
	}

	private bool _isResultSet;
	public bool IsResultSet
	{
		get => _isResultSet;
		private set => SetProperty(ref _isResultSet, value);
	}

	[BsonIgnore]
	public IRelayCommand ConfirmCommand { get; }

	[BsonIgnore]
	public IRelayCommand CancelCommand { get; }

	public void Confirm()
	{
		if (IsResultSet) return;
		IsResultSet = true;
		_tcs.TrySetResult(true);
	}

	public void Cancel()
	{
		if (IsResultSet) return;
		IsResultSet = true;
		_tcs.TrySetResult(false);
	}

	public FormsConfirmViewModel()
	{
		ConfirmCommand = new RelayCommand(Confirm, () => !IsResultSet);
		CancelCommand = new RelayCommand(Cancel, () => !IsResultSet);
	}
}
