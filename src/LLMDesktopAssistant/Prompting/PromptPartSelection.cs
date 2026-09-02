using LLMDesktopAssistant.StructuredValues.Reactive;

namespace LLMDesktopAssistant.Prompting
{
	public class PromptPartSelection : NotifyPropertyChanged
	{
		public string? Language
		{
			get;
			set => SetProperty(ref field, value);
		}

		public ReactiveNodeValue? Parameters
		{
			get;
			set => SetProperty(ref field, value);
		}
	}
}
