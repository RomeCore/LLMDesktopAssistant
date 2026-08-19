using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Tools.Meta;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// A language item with a localized display name for use in ComboBox.
/// </summary>
public class MetaToolLanguageItem
{
	/// <summary>
	/// Gets the language value.
	/// </summary>
	public ScriptLanguageType Value { get; }

	/// <summary>
	/// Gets the localized display name.
	/// </summary>
	public string DisplayName { get; }

	public MetaToolLanguageItem(ScriptLanguageType value)
	{
		Value = value;
		var key = $"settings.tools.meta_tools.language.{value.ToString().ToLower()}";
		DisplayName = LocalizationManager.LocalizeStatic(key);

		if (DisplayName == key || string.IsNullOrEmpty(DisplayName))
			DisplayName = value.ToString();
	}

	/// <summary>
	/// Gets the language items for all languages supported by meta tool engines.
	/// </summary>
	public static ImmutableList<MetaToolLanguageItem> Create(IEnumerable<IMetaToolEngine> engines) =>
		engines.Select(e => e.Language).Distinct().OrderBy(l => l.ToString())
			.Select(l => new MetaToolLanguageItem(l)).ToImmutableList();
}

/// <summary>
/// ViewModel for the create meta tool dialog: asks for the name and the language.
/// </summary>
[ViewModelFor(typeof(CreateMetaToolDialogView))]
public class CreateMetaToolDialogViewModel : NotifyPropertyChanged
{
	private string _name = string.Empty;
	private MetaToolLanguageItem? _selectedLanguage;

	/// <summary>
	/// Initializes a new instance of the <see cref="CreateMetaToolDialogViewModel"/> class.
	/// </summary>
	/// <param name="engines">The meta tool engines registered in the chat container.</param>
	public CreateMetaToolDialogViewModel(IEnumerable<IMetaToolEngine> engines)
	{
		Languages = MetaToolLanguageItem.Create(engines);
		SelectedLanguage = Languages.FirstOrDefault();

		CreateCommand = new RelayCommand(() => Close(true), () => !string.IsNullOrWhiteSpace(Name));
		CancelCommand = new RelayCommand(() => Close(false));
	}

	/// <summary>
	/// Gets the language items.
	/// </summary>
	public ImmutableList<MetaToolLanguageItem> Languages { get; }

	/// <summary>
	/// Gets or sets the tool name.
	/// </summary>
	public string Name
	{
		get => _name;
		set
		{
			if (SetProperty(ref _name, value))
				CreateCommand.NotifyCanExecuteChanged();
		}
	}

	/// <summary>
	/// Gets or sets the selected language.
	/// </summary>
	public MetaToolLanguageItem? SelectedLanguage
	{
		get => _selectedLanguage;
		set => SetProperty(ref _selectedLanguage, value);
	}

	/// <summary>
	/// Gets the task that resolves when the dialog is closed.
	/// </summary>
	public Task<bool> Result => _tcs.Task;

	/// <summary>
	/// Gets the command that submits the dialog with the entered name and language.
	/// </summary>
	public IRelayCommand CreateCommand { get; }

	/// <summary>
	/// Gets the command that cancels the dialog.
	/// </summary>
	public IRelayCommand CancelCommand { get; }

	/// <summary>
	/// Gets the dialog result: the entered name and language.
	/// </summary>
	public (string Name, ScriptLanguageType? Language) GetResult() => (Name, SelectedLanguage?.Value);

	/// <summary>
	/// Closes the dialog with the given result.
	/// </summary>
	/// <param name="result">Whether the dialog was submitted.</param>
	public void Close(bool result)
	{
		if (_isResultSet)
			return;
		_isResultSet = true;
		_tcs.TrySetResult(result);
		DialogManager.CloseDialog(result);
	}

	private readonly TaskCompletionSource<bool> _tcs = new();
	private bool _isResultSet;
}
