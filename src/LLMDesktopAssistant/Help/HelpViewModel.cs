using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Settings.Application;

namespace LLMDesktopAssistant.Help;

/// <summary>
/// View model for the built-in help viewer: exposes the documentation tree and the
/// markdown content of the currently selected document. Reacts to language changes.
/// </summary>
[ViewModelFor(typeof(HelpView))]
public class HelpViewModel : ViewModelBase
{
	private readonly HelpDocumentStore _store;
	private string _locale = string.Empty;

	/// <summary>
	/// Gets the child nodes of the documentation root, used as the tree items source.
	/// </summary>
	public IReadOnlyList<HelpDocumentNode> RootNodes => _store.Root.Children;

	private HelpDocumentNode? _selectedNode;

	/// <summary>
	/// Gets or sets the currently selected node. Selecting a document updates <see cref="MarkdownText"/>.
	/// </summary>
	public HelpDocumentNode? SelectedNode
	{
		get => _selectedNode;
		set
		{
			if (SetProperty(ref _selectedNode, value))
				UpdateMarkdown();
		}
	}

	private string _markdownText = string.Empty;

	/// <summary>
	/// Gets the markdown content of the selected document, or a placeholder when a category is selected.
	/// </summary>
	public string MarkdownText
	{
		get => _markdownText;
		private set => SetProperty(ref _markdownText, value);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HelpViewModel"/> class.
	/// </summary>
	/// <param name="store">The help document store.</param>
	public HelpViewModel(HelpDocumentStore store)
	{
		_store = store;
		_locale = GetCurrentLocale();
		_store.UpdateTitles(_locale);
		LocalizationManager.StaticLanguageChanged += OnLanguageChanged;
	}

	private void OnLanguageChanged(object? sender, string language)
	{
		_locale = GetCurrentLocale();
		_store.UpdateTitles(_locale);
		UpdateMarkdown();
	}

	private void UpdateMarkdown()
	{
		if (_selectedNode?.DocumentPath is { } path)
		{
			MarkdownText = _store.GetContent(path, _locale) ?? string.Empty;
		}
		else
		{
			MarkdownText = LocalizationManager.LocalizeStatic("help.select_document");
		}
	}

	private static string GetCurrentLocale()
	{
		return ApplicationSettingsAccessor.ApplicationSettings.Language.System;
	}
}
