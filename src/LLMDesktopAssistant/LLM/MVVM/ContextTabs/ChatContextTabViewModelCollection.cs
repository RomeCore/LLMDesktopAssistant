using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.MVVM.ContextTabs
{
	public class ChatContextTabViewModelCollection : RangeObservableCollection<ChatContextTabViewModel>
	{
		public ChatContextTabViewModelCollection()
		{
			RaiseInUIThread = true;
		}


	}
}
