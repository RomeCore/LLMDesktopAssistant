using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace LLMDesktopAssistant.Prompting
{
	public class BehaviourSlider : PromptBase
	{
		[JsonIgnore]
		public override bool IsBuiltin => PromptRegistry.BuiltinSliders.ContainsKey(Id);

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
	}
}
