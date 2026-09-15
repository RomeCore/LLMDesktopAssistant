using LLMDesktopAssistant.StructuredValues.Reactive;

namespace LLMDesktopAssistant.Addons
{
	public class AddonChangeBase : NotifyPropertyChanged
	{
		public bool? Enabled
		{
			get;
			set => SetProperty(ref field, value);
		}

		public bool? Hidden
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
