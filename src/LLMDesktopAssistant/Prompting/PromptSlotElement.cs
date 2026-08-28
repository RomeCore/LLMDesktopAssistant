namespace LLMDesktopAssistant.Prompting
{
	public class PromptSlotElement : PromptPartBase
	{
		public PromptSlotKind Kind
		{
			get;
			set => SetProperty(ref field, value);
		}
	}
}