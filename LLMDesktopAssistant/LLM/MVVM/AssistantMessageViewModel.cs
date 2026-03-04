using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.MVVM;

namespace LLMDesktopAssistant.LLM.MVVM
{
	[ViewModelFor(typeof(AssistantMessageView))]
	public class AssistantMessageViewModel : MessageViewModelBase
	{
		private ObservableCollection<AssistantMessagePartViewModel> _messageParts = [];
		public ObservableCollection<AssistantMessagePartViewModel> MessageParts
		{
			get => _messageParts;
			set => SetProperty(ref _messageParts, value);
		}

		public AssistantMessageViewModel()
		{
		}
	}
}