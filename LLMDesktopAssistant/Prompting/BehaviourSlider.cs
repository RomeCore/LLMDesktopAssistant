using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace LLMDesktopAssistant.Prompting
{
	public class BehaviourSlider : NotifyPropertyChanged
	{
		private Guid _id = Guid.NewGuid();
		/// <summary>
		/// Unique identifier for this slider.
		/// </summary>
		public Guid Id
		{
			get => _id;
			set => SetProperty(ref _id, value);
		}

		[JsonIgnore]
		public bool IsBuiltin => PromptRegistry.BuiltinSliders.ContainsKey(Id);

		private string _name = string.Empty;
		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		private int _minimumValue = 0;
		public int MinimumValue
		{
			get => _minimumValue;
			set => SetProperty(ref _minimumValue, value);
		}

		private int _maximumValue = 0;
		public int MaximumValue
		{
			get => _maximumValue;
			set => SetProperty(ref _maximumValue, value);
		}

		private int _defaultValue = 0;
		public int DefaultValue
		{
			get => _defaultValue;
			set => SetProperty(ref _defaultValue, value);
		}

		private ImmutableDictionary<int, string> _titles = [];
		public ImmutableDictionary<int, string> Titles
		{
			get => _titles;
			set => SetProperty(ref _titles, value);
		}

		private SerializableTextTemplate _template = SerializableTextTemplate.Empty;
		public SerializableTextTemplate Template
		{
			get => _template;
			set => SetProperty(ref _template, value);
		}
	}
}
