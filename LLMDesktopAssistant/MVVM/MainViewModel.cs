using CommunityToolkit.Mvvm.ComponentModel;
using LLMDesktopAssistant.LLM;
using LLMDesktopAssistant.LLM.MVVM;
using LLMDesktopAssistant.LLM.Services;

namespace LLMDesktopAssistant.MVVM
{
	public partial class MainViewModel : ViewModelBase
	{
		public ChatManagerViewModel ChatManager { get; } = new(ChatServices.ManagementService);
	}
}
