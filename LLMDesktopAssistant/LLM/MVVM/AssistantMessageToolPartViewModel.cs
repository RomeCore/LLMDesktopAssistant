using System.Collections.ObjectModel;
using LLMDesktopAssistant.MVVM;

namespace LLMDesktopAssistant.LLM.MVVM
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