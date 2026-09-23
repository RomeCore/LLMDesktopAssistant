using LLMDesktopAssistant.LLM.MVVM.Additional;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Prompting.State
{
	public class PromptStateDeltaMessageData : AdditionalChatData
	{
		public required int AnchorId { get; init; }

		public RangeObservableCollection<PromptSectionDeltaBase> Sections { get; init; } = [];
	}
}
