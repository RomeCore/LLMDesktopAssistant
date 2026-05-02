using CommunityToolkit.Mvvm.Input;
using LiteDB;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Localization;

namespace LLMDesktopAssistant.Tools.Forms;

public class InputField : NotifyPropertyChanged
{
	private string _id = string.Empty;
	public string Id
	{
		get => _id;
		set => SetProperty(ref _id, value);
	}

	private string _label = string.Empty;
	public string Label
	{
		get => _label;
		set => SetProperty(ref _label, value);
	}

	private string _placeholder = string.Empty;
	public string Placeholder
	{
		get => _placeholder;
		set => SetProperty(ref _placeholder, value);
	}

	private string _value = string.Empty;
	public string Value
	{
		get => _value;
		set => SetProperty(ref _value, value);
	}

	private string _fieldType = "text";
	/// <summary>
	/// "text", "number", "password", "multiline".
	/// </summary>
	public string FieldType
	{
		get => _fieldType;
		set => SetProperty(ref _fieldType, value);
	}

	private bool _isRequired;
	public bool IsRequired
	{
		get => _isRequired;
		set => SetProperty(ref _isRequired, value);
	}

	private string? _error;
	public string? Error
	{
		get => _error;
		set => SetProperty(ref _error, value);
	}
}

[ViewModelFor(typeof(FormsInputView))]
public class FormsInputViewModel : AdditionalMessageViewModel
{
	private readonly TaskCompletionSource<InputResult> _tcs = new();

	[BsonIgnore]
	public Task<InputResult> Result => _tcs.Task;

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

	public ImmutableList<InputField> Fields { get; }

	[BsonIgnore]
	public bool CanSubmit
	{
		get
		{
			foreach (var inputField in Fields)
			{
				if (inputField.IsRequired && string.IsNullOrWhiteSpace(inputField.Value))
					return false;
			}
			return Fields.Count > 0;
		}
	}

	private bool _isResultSet;
	public bool IsResultSet
	{
		get => _isResultSet;
		private set => SetProperty(ref _isResultSet, value);
	}

	[BsonIgnore]
	public IRelayCommand SubmitCommand { get; }

	public void OnFieldChanged()
	{
		RaisePropertyChanged(nameof(CanSubmit));
		SubmitCommand.NotifyCanExecuteChanged();
	}

	public void Submit()
	{
		if (IsResultSet || !CanSubmit) return;

		foreach (var inputField in Fields)
		{
			inputField.Error = null;
			if (inputField.IsRequired && string.IsNullOrWhiteSpace(inputField.Value))
			{
				inputField.Error = string.Format(LocalizationManager.LocalizeStatic("forms_field_required"), inputField.Label);
			}
		}

		if (Fields.Any(f => f.Error != null))
			return;

		IsResultSet = true;

		var values = new Dictionary<string, string>();
		foreach (var inputField in Fields)
		{
			values[inputField.Id] = inputField.Value.Trim();
		}

		_tcs.TrySetResult(new InputResult
		{
			Values = values
		});
	}

	[BsonCtor]
	private FormsInputViewModel()
	{
		Fields = [];
		SubmitCommand = new RelayCommand(() => { });
	}

	public FormsInputViewModel(IEnumerable<InputField> fields)
	{
		Fields = fields.ToImmutableList();
		foreach (var inputField in Fields)
			inputField.PropertyChanged += (s, e) => OnFieldChanged();
		SubmitCommand = new RelayCommand(Submit, () => CanSubmit && !IsResultSet);
	}
}

public class InputResult
{
	public required Dictionary<string, string> Values { get; init; }
}
