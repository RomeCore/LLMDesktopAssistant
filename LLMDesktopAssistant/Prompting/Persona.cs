using System.Text.Json.Serialization;

namespace LLMDesktopAssistant.Prompting
{
	public class Persona : NotifyPropertyChanged
	{
		private Guid _id = Guid.NewGuid();
		/// <summary>
		/// Unique identifier for this persona.
		/// </summary>
		public Guid Id
		{
			get => _id;
			set => SetProperty(ref _id, value);
		}

		[JsonIgnore]
		public bool IsBuiltin => PromptRegistry.BuiltinPersonas.ContainsKey(Id);

		private string _name = string.Empty;
		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		private SerializableTextTemplate _template = SerializableTextTemplate.Empty;
		public SerializableTextTemplate Template
		{
			get => _template;
			set => SetProperty(ref _template, value);
		}
	}
}