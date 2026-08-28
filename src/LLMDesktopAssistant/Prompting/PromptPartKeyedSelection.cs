namespace LLMDesktopAssistant.Prompting
{
	public class PromptPartKeyedSelection<K> : PromptPartSelection
		where K : notnull
	{
		public K Id
		{
			get;
			set => SetProperty(ref field, value);
		} = default!;
	}
}
