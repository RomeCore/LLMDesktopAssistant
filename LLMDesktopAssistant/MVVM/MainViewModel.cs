using CommunityToolkit.Mvvm.ComponentModel;
using LLMDesktopAssistant.LLM;
using LLMDesktopAssistant.LLM.MVVM;

namespace LLMDesktopAssistant.MVVM
{
	public partial class MainViewModel : ViewModelBase
	{
		public ChatManagerViewModel ChatManager { get; } = new();
	}
}
