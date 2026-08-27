using System.Text.Json.Serialization;
using DocumentFormat.OpenXml.Office.CustomUI;
using LLTSharp.Locale;
using Microsoft.CodeAnalysis;

namespace LLMDesktopAssistant.Prompting
{
	public abstract class PromptPartBase : NotifyPropertyChanged
	{
		/// <summary>
		/// Unique identifier for this prompt instance.
		/// </summary>
		public Guid Guid { get; init; } = Guid.NewGuid();

		/// <summary>
		/// Unique string identifier for this prompt instance.
		/// </summary>
		public string StrId { get; init; } = string.Empty;

		/// <summary>
		/// The language in which the prompt is written.
		/// </summary>
		public LanguageCode Language { get; init; } = LanguageCode.Invariant;

		/// <summary>
		/// The language that this prompt is extends.
		/// </summary>
		public LanguageCode? LocalizedFor { get; init; } = null;

		private string _name = string.Empty;
		/// <summary>
		/// The name of the prompt instance.
		/// </summary>
		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		private string? _description = null;
		/// <summary>
		/// The description of the prompt instance.
		/// </summary>
		public string? Description
		{
			get => _description;
			set => SetProperty(ref _description, value);
		}

		private string? _category = null;
		/// <summary>
		/// The category or type of the prompt instance.
		/// </summary>
		public string? Category
		{
			get => _category;
			set => SetProperty(ref _category, value);
		}

		private PromptPartSource _source = PromptPartSource.Unknown;
		/// <summary>
		/// The source of the prompt instance.
		/// </summary>
		[JsonIgnore]
		public PromptPartSource Source
		{
			get => _source;
			internal set => SetProperty(ref _source, value);
		}

		private PromptPartDiagnostic? _diagnostic = null;
		[JsonIgnore]
		public PromptPartDiagnostic? Diagnostic
		{
			get => _diagnostic;
			set => SetProperty(ref _diagnostic, value);
		}

		private SerializableTemplate _template = SerializableTemplate.Empty;
		public SerializableTemplate Template
		{
			get => _template;
			set => SetProperty(ref _template, value);
		}

		private PromptPartDiagnostic? _localizationDiagnostic = null;
		[JsonIgnore]
		public PromptPartDiagnostic? LocalizationDiagnostic
		{
			get => _localizationDiagnostic;
			set => SetProperty(ref _localizationDiagnostic, value);
		}

		private SerializableTemplate? _localizedTemplate = null;
		[JsonIgnore]
		public SerializableTemplate? LocalizedTemplate
		{
			get => _localizedTemplate;
			set => SetProperty(ref _localizedTemplate, value);
		}

		/// <summary>
		/// The effective template to use for this prompt instance. This is the localized template if available, otherwise the default template.
		/// </summary>
		public ITemplate EffectiveTemplate => (LocalizedTemplate ?? Template).Template;

		public PromptPartDiagnostic? CombinedDiagnostic => PromptPartDiagnostic.Combine(Diagnostic, LocalizationDiagnostic);

		public void ExpandDiagnostic(PromptPartDiagnostic diagnostic)
		{
			Diagnostic = PromptPartDiagnostic.Combine(Diagnostic, diagnostic);
		}
	}
}