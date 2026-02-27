using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.MVVM;

namespace LLMDesktopAssistant.LLM
{
	public enum ToolCallStatus
	{
		None,
		InProgress,
		Success,
		Failure
	}

	[ViewModelFor(typeof(ToolCallView))]
	public class ToolCallViewModel : ViewModelBase
	{
		private string _toolName = string.Empty;
		public string ToolName
		{
			get => _toolName;
			set => SetProperty(ref _toolName, value);
		}

		private ToolCallStatus _status = ToolCallStatus.None;
		public ToolCallStatus Status
		{
			get => _status;
			set => SetProperty(ref _status, value);
		}
	}
}