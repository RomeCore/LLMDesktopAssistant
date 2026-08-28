using LLMDesktopAssistant.Prompting.Parameterization.Values;

namespace LLMDesktopAssistant.Prompting
{
	public class PromptPartSelection : NotifyPropertyChanged
	{
		public string? Language
		{
			get;
			set => SetProperty(ref field, value);
		}

		public ParameterSchemaValue? Parameters
		{
			get;
			set => SetProperty(ref field, value);
		}
	}
}
