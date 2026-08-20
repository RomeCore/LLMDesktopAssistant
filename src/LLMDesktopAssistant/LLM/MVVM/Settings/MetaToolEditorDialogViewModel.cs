using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.LLM.Messages;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Tools.Meta;
using Material.Icons;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// A behaviour flag toggle for the meta tool editor form.
/// </summary>
public class MetaToolBehaviourToggle : NotifyPropertyChanged
{
	private bool _isChecked;

	/// <summary>
	/// Gets the behaviour flag.
	/// </summary>
	public ToolBehaviourFlagInfo FlagInfo { get; }

	/// <summary>
	/// Gets or sets a value indicating whether the flag is enabled.
	/// </summary>
	public bool IsChecked
	{
		get => _isChecked;
		set => SetProperty(ref _isChecked, value);
	}

	public MetaToolBehaviourToggle(ToolBehaviourFlagInfo info, bool isChecked)
	{
		FlagInfo = info;
		_isChecked = isChecked;
	}
}

/// <summary>
/// ViewModel for the meta tool editor dialog. Supports two modes: raw code editing with a
/// live metadata preview, and a structured form editing.
/// </summary>
[ViewModelFor(typeof(MetaToolEditorDialogView))]
public class MetaToolEditorDialogViewModel : NotifyPropertyChanged
{
	private readonly MetaToolInfo _tool;
	private readonly IMetaToolManagementService? _manager;
	private readonly IMetaToolParser _parser;
	private readonly IMetaToolEngineDescriptor _descriptor;
	private readonly IReadOnlyList<IMetaToolEngine> _engines;

	private bool _isCodeMode = true;
	private string _fileContent;
	private string _previewError = string.Empty;
	private string _previewTitle = string.Empty;
	private string _previewDescription = string.Empty;
	private string _previewCategory = string.Empty;
	private string _previewApprovalLevel = string.Empty;
	private string _previewSchema = string.Empty;
	private ImmutableList<ToolBehaviourFlagInfo> _previewBehaviours = [];
	private string _formTitle;
	private string _formDescription;
	private string _formCategory;
	private string _formSchema;
	private string _formCode;
	private ToolApprovalLevelItem? _selectedApprovalLevel;

	/// <summary>
	/// Initializes a new instance of the <see cref="MetaToolEditorDialogViewModel"/> class.
	/// </summary>
	/// <param name="tool">The tool to edit.</param>
	/// <param name="manager">The meta tool management service used to save changes.</param>
	/// <param name="engines">The meta tool engines registered in the chat container.</param>
	public MetaToolEditorDialogViewModel(MetaToolInfo tool, IMetaToolManagementService? manager,
		IEnumerable<IMetaToolEngine> engines)
	{
		_tool = tool;
		_manager = manager;
		_parser = ServiceRegistry.Provider.GetRequiredService<IMetaToolParser>();
		_engines = engines.ToArray();

		var extension = Path.GetExtension(tool.Path ?? string.Empty);
		_descriptor = _engines
			.FirstOrDefault(e => e.Descriptor.Extensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
			?.Descriptor
			?? throw new NotSupportedException($"No engine found for extension '{extension}'.");

		_fileContent = tool.Path is not null && File.Exists(tool.Path) ? File.ReadAllText(tool.Path) : string.Empty;

		_formTitle = tool.Title;
		_formDescription = tool.Description;
		_formCategory = tool.Category;
		_selectedApprovalLevel = ToolApprovalLevelItem.All.FirstOrDefault(i => i.Value == tool.ApprovalLevel)
			?? ToolApprovalLevelItem.All[0];
		_formSchema = tool.ArgumentSchema?.ToJsonString(new JsonSerializerOptions
		{
			Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
			WriteIndented = true
		}) ?? "{}";
		_formCode = tool.ExecutionCode;

		BehaviourToggles = Enum.GetValues<ToolBehaviour>()
			.Where(f => f is not ToolBehaviour.None and not ToolBehaviour.All
				and not ToolBehaviour.AdHoc and not ToolBehaviour.MCP and not ToolBehaviour.Meta)
			.Select(f => new MetaToolBehaviourToggle(ToolBehaviourFlagInfo.Create(f), tool.Behaviours.HasFlag(f)))
			.ToImmutableList();

		ToggleModeCommand = new RelayCommand(ToggleMode);
		SaveCommand = new AsyncRelayCommand(SaveAsync);
		CancelCommand = new RelayCommand(() => Close(false));

		UpdatePreview();
	}

	/// <summary>
	/// Gets the name of the tool.
	/// </summary>
	public string Name => _tool.Name;

	/// <summary>
	/// Gets the file path of the tool.
	/// </summary>
	public string FilePath => _tool.Path ?? string.Empty;

	/// <summary>
	/// Gets the script language of the tool.
	/// </summary>
	public ScriptLanguageType Language => _tool.ScriptLanguage;

	/// <summary>
	/// Gets the localized display name of the language.
	/// </summary>
	public string LanguageName => LocalizeLanguage(_tool.ScriptLanguage);

	/// <summary>
	/// Gets the icon of the language.
	/// </summary>
	public MaterialIconKind LanguageIcon => _tool.ScriptLanguage switch
	{
		ScriptLanguageType.Lua => MaterialIconKind.LanguageLua,
		ScriptLanguageType.Python => MaterialIconKind.LanguagePython,
		ScriptLanguageType.CSharpScript => MaterialIconKind.LanguageCsharp,
		_ => MaterialIconKind.FileCode
	};

	/// <summary>
	/// Gets a value indicating whether the code editing mode is active.
	/// </summary>
	public bool IsCodeMode
	{
		get => _isCodeMode;
		private set => SetProperty(ref _isCodeMode, value);
	}

	// ─────────────────────────── Code mode ───────────────────────────

	/// <summary>
	/// Gets or sets the raw file contents.
	/// </summary>
	public string FileContent
	{
		get => _fileContent;
		set
		{
			if (SetProperty(ref _fileContent, value))
				UpdatePreview();
		}
	}

	/// <summary>
	/// Gets the preview error message, or <see cref="string.Empty"/> when the file parses.
	/// </summary>
	public string PreviewError
	{
		get => _previewError;
		private set
		{
			if (SetProperty(ref _previewError, value))
				OnPropertyChanged(nameof(HasPreviewError));
		}
	}

	/// <summary>
	/// Gets a value indicating whether the preview contains an error.
	/// </summary>
	public bool HasPreviewError => !string.IsNullOrEmpty(PreviewError);

	/// <summary>
	/// Gets the previewed title.
	/// </summary>
	public string PreviewTitle
	{
		get => _previewTitle;
		private set => SetProperty(ref _previewTitle, value);
	}

	/// <summary>
	/// Gets the previewed description.
	/// </summary>
	public string PreviewDescription
	{
		get => _previewDescription;
		private set => SetProperty(ref _previewDescription, value);
	}

	/// <summary>
	/// Gets the previewed category.
	/// </summary>
	public string PreviewCategory
	{
		get => _previewCategory;
		private set => SetProperty(ref _previewCategory, value);
	}

	/// <summary>
	/// Gets the previewed approval level.
	/// </summary>
	public string PreviewApprovalLevel
	{
		get => _previewApprovalLevel;
		private set => SetProperty(ref _previewApprovalLevel, value);
	}

	/// <summary>
	/// Gets the previewed argument schema JSON.
	/// </summary>
	public string PreviewSchema
	{
		get => _previewSchema;
		private set => SetProperty(ref _previewSchema, value);
	}

	/// <summary>
	/// Gets the previewed behaviour flags.
	/// </summary>
	public ImmutableList<ToolBehaviourFlagInfo> PreviewBehaviours
	{
		get => _previewBehaviours;
		private set => SetProperty(ref _previewBehaviours, value);
	}

	// ─────────────────────────── Form mode ───────────────────────────

	/// <summary>
	/// Gets or sets the form title.
	/// </summary>
	public string FormTitle
	{
		get => _formTitle;
		set => SetProperty(ref _formTitle, value);
	}

	/// <summary>
	/// Gets or sets the form description.
	/// </summary>
	public string FormDescription
	{
		get => _formDescription;
		set => SetProperty(ref _formDescription, value);
	}

	/// <summary>
	/// Gets or sets the form category.
	/// </summary>
	public string FormCategory
	{
		get => _formCategory;
		set => SetProperty(ref _formCategory, value);
	}

	/// <summary>
	/// Gets the approval level items.
	/// </summary>
	public ImmutableList<ToolApprovalLevelItem> ApprovalLevelItems => ToolApprovalLevelItem.All;

	/// <summary>
	/// Gets or sets the selected approval level.
	/// </summary>
	public ToolApprovalLevelItem? SelectedApprovalLevel
	{
		get => _selectedApprovalLevel;
		set => SetProperty(ref _selectedApprovalLevel, value);
	}

	/// <summary>
	/// Gets the behaviour flag toggles.
	/// </summary>
	public ImmutableList<MetaToolBehaviourToggle> BehaviourToggles { get; }

	/// <summary>
	/// Gets or sets the form argument schema JSON.
	/// </summary>
	public string FormSchema
	{
		get => _formSchema;
		set => SetProperty(ref _formSchema, value);
	}

	/// <summary>
	/// Gets or sets the form execution code (without the frontmatter).
	/// </summary>
	public string FormCode
	{
		get => _formCode;
		set => SetProperty(ref _formCode, value);
	}

	// ─────────────────────────── Commands ───────────────────────────

	/// <summary>
	/// Gets the command that toggles between code and form modes.
	/// </summary>
	public ICommand ToggleModeCommand { get; }

	/// <summary>
	/// Gets the command that saves the tool and closes the dialog.
	/// </summary>
	public ICommand SaveCommand { get; }

	/// <summary>
	/// Gets the command that cancels the dialog.
	/// </summary>
	public ICommand CancelCommand { get; }

	/// <summary>
	/// Closes the dialog with the given result.
	/// </summary>
	/// <param name="result">Whether the tool was saved.</param>
	public void Close(bool result)
	{
		if (_isResultSet)
			return;
		_isResultSet = true;
		DialogManager.CloseDialog(result);
	}

	private bool _isResultSet;

	private void ToggleMode() => IsCodeMode = !IsCodeMode;

	private void UpdatePreview()
	{
		try
		{
			var info = _parser.Parse(FilePath, FileContent, _tool.Source, _descriptor);

			if (info.Diagnostic?.IsFatal == true)
			{
				PreviewError = info.Diagnostic.Exception?.Message
					?? LocalizationManager.LocalizeStatic("settings.tools.meta_tools.editor.preview.invalid");
				PreviewTitle = PreviewDescription = PreviewCategory = PreviewApprovalLevel = PreviewSchema = string.Empty;
				PreviewBehaviours = [];
				return;
			}

			PreviewError = string.Empty;
			PreviewTitle = info.Title;
			PreviewDescription = info.Description;
			PreviewCategory = info.Category;
			PreviewApprovalLevel = LocalizeApprovalLevel(info.ApprovalLevel);
			PreviewSchema = info.ArgumentSchema?.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) ?? "{}";
			PreviewBehaviours = ToolBehaviourFlagInfo.CreateForFlags(info.Behaviours);
		}
		catch (Exception ex)
		{
			PreviewError = ex.Message;
		}
	}

	private async Task SaveAsync()
	{
		if (_manager is null)
			return;

		try
		{
			if (IsCodeMode)
			{
				_manager.SaveToolFile(Name, FileContent);
			}
			else
			{
				JsonObject? schema;
				try
				{
					schema = JsonSerializer.Deserialize<JsonObject>(FormSchema);
				}
				catch (Exception ex)
				{
					ShowError(LocalizationManager.LocalizeStatic("settings.tools.meta_tools.editor.error.invalid_schema"), ex.Message);
					return;
				}
				
				var behaviours = ToolBehaviour.None;
				foreach (var toggle in BehaviourToggles)
				{
					if (toggle.IsChecked)
						behaviours |= toggle.FlagInfo.Flag;
				}

				_manager.CreateOrUpdateTool(Name, null, FormDescription, FormTitle, FormCategory,
					SelectedApprovalLevel?.Value, behaviours, schema, null, FormCode);
			}

			Close(true);
		}
		catch (Exception ex)
		{
			ShowError(LocalizationManager.LocalizeStatic("common.error"), ex.Message);
		}
	}

	private static void ShowError(string title, string message)
	{
		ServiceRegistry.Provider.GetRequiredService<IToastService>()
			.ShowError(title, message);
	}

	private static string LocalizeLanguage(ScriptLanguageType language)
	{
		var key = $"settings.tools.meta_tools.language.{language.ToString().ToLower()}";
		var localized = LocalizationManager.LocalizeStatic(key);
		return localized == key ? language.ToString() : localized;
	}

	private static string LocalizeApprovalLevel(ToolApprovalLevel level)
	{
		return ToolApprovalLevelItem.All.FirstOrDefault(i => i.Value == level)?.DisplayName ?? level.ToString();
	}
}
