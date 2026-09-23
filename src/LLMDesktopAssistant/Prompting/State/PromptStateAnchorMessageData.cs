using LLMDesktopAssistant.LLM.MVVM.Additional;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Prompting.State
{
	public class PromptStateAnchorMessageData : AdditionalChatData
	{
		public required int Id { get; init; }

		public RangeObservableCollection<PromptSectionStateBase> Sections { get; init; } = [];
	}
}
