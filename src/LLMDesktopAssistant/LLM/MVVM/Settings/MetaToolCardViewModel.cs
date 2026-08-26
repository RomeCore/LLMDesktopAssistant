using System.Diagnostics;
using System.Text.Json;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.Controls.Dialogs;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Tools.Meta;
using Material.Icons;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// ViewModel for a single meta tool card in the chat tools settings.
/// </summary>
public class MetaToolCardViewModel : ViewModelBase
{
	private readonly MetaToolInfo _info;
	private readonly IMetaToolManagementService? _manager;
	private readonly IExplorerOpener? _explorerOpener;
	private readonly Action? _onChanged;
	private readonly Action<MetaToolCardViewModel>? _onEdit;
	private bool _isDetailsVisible;

	/// <summary>
	/// Initializes a new instance of the <see cref="MetaToolCardViewModel"/> class.
	/// </summary>
	/// <param name="info">The meta tool info to display.</param>
	/// <param name="manager">The meta tool management service used for rename, duplicate and delete operations.</param>
	/// <param name="onChanged">A callback invoked after the tool was renamed, duplicated or deleted.</param>
	/// <param name="onEdit">A callback invoked when the user requests to edit the tool.</param>
	public MetaToolCardViewModel(MetaToolInfo info, IMetaToolManagementService? manager,
		Action? onChanged = null, Action<MetaToolCardViewModel>? onEdit = null)
	{
		_info = info;
		_manager = manager;
		_onChanged = onChanged;
		_onEdit = onEdit;
		_explorerOpener = ServiceRegistry.Provider.GetService<IExplorerOpener>();

		DiagnosticFlags = info.Diagnostic is null
			? []
			: MetaToolDiagnosticFlagInfo.CreateForCodes(info.Diagnostic.Codes);

		BehaviourFlags = ToolBehaviourFlagInfo.CreateForFlags(info.Behaviours);
		ArgumentSchemaText = info.ArgumentSchema is null
			? null
			: JsonSerializer.Serialize(info.ArgumentSchema, new JsonSerializerOptions { WriteIndented = true });

		ToggleDetailsCommand = new RelayCommand(() => IsDetailsVisible = !IsDetailsVisible);
		EditCommand = new RelayCommand(() => _onEdit?.Invoke(this));
		OpenFileCommand = new RelayCommand(OpenFile, () => HasPath);
		ShowInExplorerCommand = new RelayCommand(ShowInExplorer, () => HasPath);
		RenameCommand = new AsyncRelayCommand(RenameAsync, () => HasPath);
		DuplicateCommand = new AsyncRelayCommand(DuplicateAsync, () => HasPath);
		DeleteCommand = new AsyncRelayCommand(DeleteAsync, () => HasPath);
	}

	/// <summary>
	/// Gets the underlying <see cref="MetaToolInfo"/>.
	/// </summary>
	public MetaToolInfo Info => _info;

	/// <summary>
	/// Gets the name of the tool.
	/// </summary>
	public string Name => _info.Name;

	/// <summary>
	/// Gets the title of the tool (falls back to the name).
	/// </summary>
	public string Title => string.IsNullOrEmpty(_info.Title) ? _info.Name : _info.Title;

	/// <summary>
	/// Gets the description of the tool.
	/// </summary>
	public string Description => _info.Description;

	/// <summary>
	/// Gets the category of the tool.
	/// </summary>
	public string Category => _info.Category;

	/// <summary>
	/// Gets the localized display name of the approval level.
	/// </summary>
	public string? ApprovalLevelName => ToolApprovalLevelItem.AllWithDefault.FirstOrDefault(i => i.Value == _info.ApprovalLevel)?.DisplayName;

	/// <summary>
	/// Gets the behaviour flag chips of the tool.
	/// </summary>
	public ImmutableList<ToolBehaviourFlagInfo> BehaviourFlags { get; }

	/// <summary>
	/// Gets the pretty-printed argument schema JSON, or <see langword="null"/> when absent.
	/// </summary>
	public string? ArgumentSchemaText { get; }

	/// <summary>
	/// Gets the script language of the tool.
	/// </summary>
	public ScriptLanguageType Language => _info.ScriptLanguage;

	/// <summary>
	/// Gets the localized display name of the script language.
	/// </summary>
	public string LanguageName => LocalizeLanguage(_info.ScriptLanguage);

	/// <summary>
	/// Gets the icon of the script language.
	/// </summary>
	public MaterialIconKind LanguageIcon => _info.ScriptLanguage switch
	{
		ScriptLanguageType.Lua => MaterialIconKind.LanguageLua,
		ScriptLanguageType.Python => MaterialIconKind.LanguagePython,
		ScriptLanguageType.CSharpScript => MaterialIconKind.LanguageCsharp,
		_ => MaterialIconKind.FileCode
	};

	/// <summary>
	/// Gets the source of the tool file.
	/// </summary>
	public MetaToolSource Source => _info.Source;

	/// <summary>
	/// Gets the localized display name of the source.
	/// </summary>
	public string SourceName => LocalizeSource(_info.Source);

	/// <summary>
	/// Gets the icon of the source.
	/// </summary>
	public MaterialIconKind SourceIcon => _info.Source switch
	{
		MetaToolSource.UserProfile => MaterialIconKind.AccountCircle,
		MetaToolSource.WorkingDirectory => MaterialIconKind.Folder,
		MetaToolSource.Custom => MaterialIconKind.FolderStar,
		_ => MaterialIconKind.HelpCircle
	};

	/// <summary>
	/// Gets the file path of the tool.
	/// </summary>
	public string FilePath => _info.Path ?? string.Empty;

	/// <summary>
	/// Gets the file name of the tool.
	/// </summary>
	public string FileName => Path.GetFileName(FilePath);

	/// <summary>
	/// Gets a value indicating whether the tool file exists on disk.
	/// </summary>
	public bool HasPath => !string.IsNullOrEmpty(_info.Path) && File.Exists(_info.Path);

	/// <summary>
	/// Gets a value indicating whether the tool is unusable (fatal diagnostic).
	/// </summary>
	public bool IsBroken => _info.Diagnostic?.IsFatal == true;

	/// <summary>
	/// Gets the error message of a fatal diagnostic, if any.
	/// </summary>
	public string? Error => _info.Diagnostic?.Exception?.Message;

	/// <summary>
	/// Gets the diagnostic flag chips of the tool.
	/// </summary>
	public ImmutableList<MetaToolDiagnosticFlagInfo> DiagnosticFlags { get; }

	/// <summary>
	/// Gets a value indicating whether the tool has any diagnostic flags.
	/// </summary>
	public bool HasDiagnostics => DiagnosticFlags.Count > 0;

	/// <summary>
	/// Gets or sets a value indicating whether the details section is expanded.
	/// </summary>
	public bool IsDetailsVisible
	{
		get => _isDetailsVisible;
		set => SetProperty(ref _isDetailsVisible, value);
	}

	/// <summary>
	/// Gets the command that opens the tool in the editor.
	/// </summary>
	public ICommand EditCommand { get; }

	/// <summary>
	/// Gets the command that opens the tool file with the default application.
	/// </summary>
	public ICommand OpenFileCommand { get; }

	/// <summary>
	/// Gets the command that shows the tool file in the system explorer.
	/// </summary>
	public ICommand ShowInExplorerCommand { get; }

	/// <summary>
	/// Gets the command that renames the tool.
	/// </summary>
	public ICommand RenameCommand { get; }

	/// <summary>
	/// Gets the command that duplicates the tool under a new name.
	/// </summary>
	public ICommand DuplicateCommand { get; }

	/// <summary>
	/// Gets the command that deletes the tool file.
	/// </summary>
	public ICommand DeleteCommand { get; }

	/// <summary>
	/// Gets the command that toggles the details section.
	/// </summary>
	public ICommand ToggleDetailsCommand { get; }

	private void OpenFile()
	{
		if (!HasPath)
			return;

		try
		{
			Process.Start(new ProcessStartInfo(FilePath) { UseShellExecute = true });
		}
		catch (Exception ex)
		{
			ShowError(ex.Message);
		}
	}

	private void ShowInExplorer()
	{
		if (HasPath)
			_explorerOpener?.OpenPath(FilePath);
	}

	private async Task RenameAsync()
	{
		if (!HasPath)
			return;

		var dialog = new TextInputDialogViewModel
		{
			Title = LocalizationManager.LocalizeStatic("settings.tools.meta_tools.rename.title"),
			Description = LocalizationManager.LocalizeStatic("settings.tools.meta_tools.rename.description"),
			Label = LocalizationManager.LocalizeStatic("settings.tools.meta_tools.rename.label"),
			Placeholder = LocalizationManager.LocalizeStatic("settings.tools.meta_tools.rename.placeholder"),
			Value = Name,
			SubmitText = LocalizationManager.LocalizeStatic("common.rename"),
			CancelText = LocalizationManager.LocalizeStatic("common.cancel"),
			IsRequired = true
		};

		var newName = (string?)await DialogManager.ShowDialogAsync(dialog);
		if (string.IsNullOrEmpty(newName) || newName == Name)
			return;

		var toast = ServiceRegistry.Provider.GetRequiredService<IToastService>();
		if (!ToolName.CheckValid(newName))
		{
			toast.ShowError(LocalizationManager.LocalizeStatic("settings.tools.meta_tools.rename.title"),
				LocalizationManager.LocalizeStatic("settings.tools.meta_tools.rename.error.invalid"));
			return;
		}

		try
		{
			_manager?.RenameTool(Name, newName);
			_onChanged?.Invoke();
		}
		catch (Exception ex)
		{
			ShowError(ex.Message);
		}
	}

	private async Task DuplicateAsync()
	{
		if (!HasPath)
			return;

		var dialog = new TextInputDialogViewModel
		{
			Title = LocalizationManager.LocalizeStatic("settings.tools.meta_tools.duplicate.title"),
			Description = LocalizationManager.LocalizeStatic("settings.tools.meta_tools.duplicate.description"),
			Label = LocalizationManager.LocalizeStatic("settings.tools.meta_tools.duplicate.label"),
			Placeholder = LocalizationManager.LocalizeStatic("settings.tools.meta_tools.duplicate.placeholder"),
			Value = Name + "_copy",
			SubmitText = LocalizationManager.LocalizeStatic("common.duplicate"),
			CancelText = LocalizationManager.LocalizeStatic("common.cancel"),
			IsRequired = true
		};

		var newName = (string?)await DialogManager.ShowDialogAsync(dialog);
		if (string.IsNullOrEmpty(newName) || newName == Name)
			return;

		var toast = ServiceRegistry.Provider.GetRequiredService<IToastService>();
		if (!ToolName.CheckValid(newName))
		{
			toast.ShowError(LocalizationManager.LocalizeStatic("settings.tools.meta_tools.duplicate.title"),
				LocalizationManager.LocalizeStatic("settings.tools.meta_tools.rename.error.invalid"));
			return;
		}

		try
		{
			var extension = Path.GetExtension(FilePath);
			var newPath = Path.Combine(Path.GetDirectoryName(FilePath)!, newName + extension);
			if (File.Exists(newPath))
			{
				toast.ShowError(LocalizationManager.LocalizeStatic("settings.tools.meta_tools.duplicate.title"),
					LocalizationManager.LocalizeStatic("settings.tools.meta_tools.rename.error.exists"));
				return;
			}

			File.Copy(FilePath, newPath);
			_onChanged?.Invoke();
		}
		catch (Exception ex)
		{
			ShowError(ex.Message);
		}
	}

	private async Task DeleteAsync()
	{
		if (!HasPath)
			return;

		var confirm = new ConfirmDialogViewModel
		{
			Title = LocalizationManager.LocalizeStatic("settings.tools.meta_tools.delete.title"),
			Description = LocalizationManager.LocalizeStaticFormat("settings.tools.meta_tools.delete.confirm", FilePath),
			ConfirmText = LocalizationManager.LocalizeStatic("common.delete"),
			CancelText = LocalizationManager.LocalizeStatic("common.cancel"),
			IsDanger = true
		};

#pragma warning disable CS8605 // Unboxing is safe: the confirmation dialog always returns bool.
		var confirmed = (bool)await DialogManager.ShowDialogAsync(confirm);
#pragma warning restore CS8605
		if (!confirmed)
			return;

		try
		{
			_manager?.DeleteTool(Name);
			_onChanged?.Invoke();
		}
		catch (Exception ex)
		{
			ShowError(ex.Message);
		}
	}

	private static void ShowError(string message)
	{
		ServiceRegistry.Provider.GetRequiredService<IToastService>()
			.ShowError(LocalizationManager.LocalizeStatic("common.error"), message);
	}

	private static string LocalizeLanguage(ScriptLanguageType language)
	{
		var key = $"settings.tools.meta_tools.language.{language.ToString().ToLower()}";
		var localized = LocalizationManager.LocalizeStatic(key);
		return localized == key ? language.ToString() : localized;
	}

	private static string LocalizeSource(MetaToolSource source)
	{
		var key = $"settings.tools.meta_tools.scope.{source.ToString().ToLower()}";
		var localized = LocalizationManager.LocalizeStatic(key);
		return localized == key ? source.ToString() : localized;
	}
}
