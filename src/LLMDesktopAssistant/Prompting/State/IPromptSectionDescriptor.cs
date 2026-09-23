namespace LLMDesktopAssistant.Prompting.State
{
	public interface IPromptSectionDescriptor
	{
		Type StateType { get; }

		Type DeltaType { get; }

		int Order { get; }
	}
}
