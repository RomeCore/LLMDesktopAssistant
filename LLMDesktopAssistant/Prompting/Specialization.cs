using System.Text.Json.Serialization;

namespace LLMDesktopAssistant.Prompting
{
	public class Specialization : NotifyPropertyChanged
	{
		private Guid _id = Guid.NewGuid();
		/// <summary>
		/// Unique identifier for this specialization.
		/// </summary>
		public Guid Id
		{
			get => _id;
			set => SetProperty(ref _id, value);
		}

		[JsonIgnore]
		public bool IsBuiltin => PromptRegistry.BuiltinSpecializations.ContainsKey(Id);

		private string _name = string.Empty;
		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		private string _category = string.Empty;
		public string Category
		{
			get => _category;
			set => SetProperty(ref _category, value);
		}

		private SerializableTextTemplate _template = SerializableTextTemplate.Empty;
		public SerializableTextTemplate Template
		{
			get => _template;
			set => SetProperty(ref _template, value);
		}
	}
}
