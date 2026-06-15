using LLTSharp.Locale;
using System.Text.Json.Serialization;

namespace LLMDesktopAssistant.Prompting
{
	public abstract class PromptBase : NotifyPropertyChanged
	{
		private Guid _id = Guid.NewGuid();
		/// <summary>
		/// Unique identifier for this prompt instance.
		/// </summary>
		public Guid Id
		{
			get => _id;
			set => SetProperty(ref _id, value);
		}

		private string _name = string.Empty;
		/// <summary>
		/// The name of the prompt instance.
		/// </summary>
		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		private string _category = string.Empty;
		/// <summary>
		/// The category or type of the prompt instance.
		/// </summary>
		public string Category
		{
			get => _category;
			set => SetProperty(ref _category, value);
		}

		private LanguageCode _language = LanguageCode.English;
		/// <summary>
		/// The language in which the prompt is written.
		/// </summary>
		public LanguageCode Language
		{
			get => _language;
			set => SetProperty(ref _language, value);
		}

		private LanguageCode? _localizedFor = null;
		/// <summary>
		/// The language that this prompt is extends.
		/// </summary>
		public LanguageCode? LocalizedFor
		{
			get => _localizedFor;
			set => SetProperty(ref _localizedFor, value);
		}

		private SerializableTextTemplate _template = SerializableTextTemplate.Empty;
		public SerializableTextTemplate Template
		{
			get => _template;
			set => SetProperty(ref _template, value);
		}
	}
}