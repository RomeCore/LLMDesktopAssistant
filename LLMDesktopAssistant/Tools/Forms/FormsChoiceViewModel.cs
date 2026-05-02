using LLMDesktopAssistant.LLM.Domain;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using LiteDB;

namespace LLMDesktopAssistant.Tools.Forms;

public class ChoiceOption : NotifyPropertyChanged
{
	private string _value = string.Empty;
	public string Value
	{
		get => _value;
		set => SetProperty(ref _value, value);
	}

	private string _label = string.Empty;
	public string Label
	{
		get => _label;
		set => SetProperty(ref _label, value);
	}

	private bool _isSelected;
	public bool IsSelected
	{
		get => _isSelected;
		set => SetProperty(ref _isSelected, value);
	}
}

[ViewModelFor(typeof(FormsChoiceView))]
public class FormsChoiceViewModel : AdditionalMessageViewModel
{
	private readonly TaskCompletionSource<ChoiceResult> _tcs = new();

	[BsonIgnore]
	public Task<ChoiceResult> Result => _tcs.Task;

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

	public ImmutableList<ChoiceOption> Options { get; }

	private bool _allowMultiple;
	public bool AllowMultiple
	{
		get => _allowMultiple;
		set => SetProperty(ref _allowMultiple, value);
	}

	private bool _allowCustom;
	public bool AllowCustom
	{
		get => _allowCustom;
		set => SetProperty(ref _allowCustom, value);
	}

	private int _minSelect;
	public int MinSelect
	{
		get => _minSelect;
		set => SetProperty(ref _minSelect, value);
	}

	private int _maxSelect;
	public int MaxSelect
	{
		get => _maxSelect;
		set => SetProperty(ref _maxSelect, value);
	}

	private string _customInput = string.Empty;
	public string CustomInput
	{
		get => _customInput;
		set
		{
			if (SetProperty(ref _customInput, value))
			{
				RaisePropertyChanged(nameof(CanSubmit));
				SubmitCommand.NotifyCanExecuteChanged();
			}
		}
	}

	[BsonIgnore]
	public bool CanSubmit
	{
		get
		{
			var selectedCount = Options.Count(o => o.IsSelected);
			if (AllowCustom && !string.IsNullOrWhiteSpace(CustomInput))
				selectedCount++;

			return selectedCount >= MinSelect && selectedCount <= MaxSelect && selectedCount > 0;
		}
	}

	private bool _isResultSet;
	public bool IsResultSet
	{
		get => _isResultSet;
		private set => SetProperty(ref _isResultSet, value);
	}

	[BsonIgnore]
	public IRelayCommand ToggleCommand { get; }

	[BsonIgnore]
	public IRelayCommand SubmitCommand { get; }

	public void ToggleOption(ChoiceOption? option)
	{
		if (IsResultSet || option == null) return;

		if (!AllowMultiple)
		{
			foreach (var o in Options)
				o.IsSelected = o == option ? o.IsSelected : false;
		}

		RaisePropertyChanged(nameof(CanSubmit));
		SubmitCommand.NotifyCanExecuteChanged();
	}

	public void Submit()
	{
		if (IsResultSet || !CanSubmit) return;
		IsResultSet = true;

		var selected = Options
			.Where(o => o.IsSelected)
			.Select(o => o.Value)
			.ToList();

		if (AllowCustom && !string.IsNullOrWhiteSpace(CustomInput))
			selected.Add(CustomInput.Trim());

		_tcs.TrySetResult(new ChoiceResult
		{
			Selected = [.. selected],
			Custom = AllowCustom ? CustomInput?.Trim() : null
		});
	}

	[BsonCtor]
	private FormsChoiceViewModel()
	{
		Options = [];
		ToggleCommand = new RelayCommand<ChoiceOption>(ToggleOption);
		SubmitCommand = new RelayCommand(Submit, () => CanSubmit && !IsResultSet);
	}

	public FormsChoiceViewModel(IEnumerable<ChoiceOption> options)
	{
		Options = options.ToImmutableList();
		ToggleCommand = new RelayCommand<ChoiceOption>(ToggleOption);
		SubmitCommand = new RelayCommand(Submit, () => CanSubmit && !IsResultSet);
	}
}

public class ChoiceResult
{
	public required string[] Selected { get; init; }

	public string? Custom { get; init; }
}
