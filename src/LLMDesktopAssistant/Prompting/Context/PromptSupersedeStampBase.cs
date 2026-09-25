namespace LLMDesktopAssistant.Prompting.Context
{
	public class PromptSupersedeStampBase : NotifyPropertyChanged
	{
		internal string Discriminator
		{
			get;
			set => SetProperty(ref field, value);
		} = string.Empty;

		internal string Snapshot
		{
			get;
			set => SetProperty(ref field, value);
		} = string.Empty;
	}
}
