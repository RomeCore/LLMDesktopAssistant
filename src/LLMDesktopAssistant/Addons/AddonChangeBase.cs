using LLMDesktopAssistant.StructuredValues.Reactive;

namespace LLMDesktopAssistant.Addons
{
	public class AddonChangeBase : NotifyPropertyChanged
	{
		public string Name
		{
			get;
			set => SetProperty(ref field, value);
		} = string.Empty;

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
