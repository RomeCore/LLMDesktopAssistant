using System.Collections.ObjectModel;
using LLMDesktopAssistant.Avalonia.LLM.Messages;
using LLMDesktopAssistant.Core.MVVM;

namespace LLMDesktopAssistant.Avalonia.LLM.Messages
{
	[ViewModelFor(typeof(AssistantMessageToolPartView))]
	public class AssistantMessageToolPartViewModel : AssistantMessagePartViewModel
	{
		private ObservableCollection<ToolCallViewModel> _toolCalls = [];
		public ObservableCollection<ToolCallViewModel> ToolCalls
		{
			get => _toolCalls;
			set => SetProperty(ref _toolCalls, value);
		}

		public AssistantMessageToolPartViewModel()
		{
		}
	}
}