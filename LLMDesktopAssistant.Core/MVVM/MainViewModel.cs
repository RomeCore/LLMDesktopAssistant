using CommunityToolkit.Mvvm.ComponentModel;
using LLMDesktopAssistant.LLM;

namespace LLMDesktopAssistant.MVVM
{
	public partial class MainViewModel : ViewModelBase
	{
		public ChatManagerViewModel ChatManager { get; } = new();
	}
}
