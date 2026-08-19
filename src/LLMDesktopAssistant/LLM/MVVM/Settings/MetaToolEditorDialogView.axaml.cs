using Avalonia.Controls;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using LLMDesktopAssistant.Scripting;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using TextMateSharp.Grammars;

namespace LLMDesktopAssistant.LLM.MVVM.Settings;

/// <summary>
/// The view for the meta tool editor dialog with AvaloniaEdit-based code editors
/// and TextMate syntax highlighting.
/// </summary>
public partial class MetaToolEditorDialogView : UserControl
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MetaToolEditorDialogView"/> class.
	/// </summary>
	public MetaToolEditorDialogView()
	{
		InitializeComponent();
		DataContextChanged += OnDataContextChanged;
	}

	private void OnDataContextChanged(object? sender, EventArgs e)
	{
		if (DataContext is MetaToolEditorDialogViewModel vm)
		{
			ConfigureEditor(CodeEditor, vm.Language);
			ConfigureEditor(FormCodeEditor, vm.Language);
			BindText(CodeEditor, () => vm.FileContent, v => vm.FileContent = v, vm, nameof(MetaToolEditorDialogViewModel.FileContent));
			BindText(FormCodeEditor, () => vm.FormCode, v => vm.FormCode = v, vm, nameof(MetaToolEditorDialogViewModel.FormCode));
		}
	}

	/// <summary>
	/// Synchronizes the editor text with the view model property in both directions.
	/// <see cref="TextEditor.Text"/> is not bindable, so manual synchronization is required.
	/// </summary>
	private static void BindText(TextEditor editor, Func<string> getter, Action<string> setter,
		MetaToolEditorDialogViewModel vm, string propertyName)
	{
		editor.Text = getter();
		editor.TextChanged += (_, _) =>
		{
			var text = editor.Text;
			if (getter() != text)
				setter(text);
		};
		vm.PropertyChanged += (_, args) =>
		{
			if (args.PropertyName == propertyName && editor.Text != getter())
				editor.Text = getter();
		};
	}

	private static void ConfigureEditor(TextEditor editor, ScriptLanguageType language)
	{
		try
		{
			var registryOptions = new RegistryOptions(ThemeName.DarkPlus);
			ServiceRegistry.Provider.GetRequiredService<TextMateLoader>().LoadGrammarsInto(registryOptions);

			var languageId = language switch
			{
				ScriptLanguageType.Lua => "lua",
				ScriptLanguageType.Python => "python",
				ScriptLanguageType.CSharpScript => "csharp",
				_ => "plaintext"
			};

			var installation = editor.InstallTextMate(registryOptions);
			installation.SetGrammar(registryOptions.GetScopeByLanguageId(languageId));
		}
		catch
		{
			// Syntax highlighting is optional; the editor works without it.
		}
	}
}
