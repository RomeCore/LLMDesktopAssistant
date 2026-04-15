using CommunityToolkit.Mvvm.ComponentModel;
using LLMDesktopAssistant.Avalonia.LLM;

namespace LLMDesktopAssistant.Avalonia.MVVM
{
	public partial class MainViewModel : ViewModelBase
	{
		public ChatManagerViewModel ChatManager { get; } = new();
	}
}
