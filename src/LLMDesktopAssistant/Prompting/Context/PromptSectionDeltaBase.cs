namespace LLMDesktopAssistant.Prompting.Context
{
	public class PromptSectionDeltaBase : NotifyPropertyChanged
	{
		internal string Discriminator
		{
			get;
			set => SetProperty(ref field, value);
		} = string.Empty;
	}

}
