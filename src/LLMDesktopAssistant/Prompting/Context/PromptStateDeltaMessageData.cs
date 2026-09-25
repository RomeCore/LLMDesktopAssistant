using LLMDesktopAssistant.LLM.MVVM.Additional;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Prompting.Context
{
	public class PromptStateDeltaMessageData : AdditionalChatData
	{
		public required int AnchorId { get; init; }

		public RangeObservableCollection<PromptSectionDeltaBase> Sections { get; init; } = [];

		public required string Snapshot { get; init; }

		public PromptStateDeltaMessageData()
		{
			IsVisible = false;
		}
	}
}
