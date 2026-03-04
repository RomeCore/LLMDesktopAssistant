using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LLMDesktopAssistant.MVVM;

namespace LLMDesktopAssistant.LLM.MVVM
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
		private ToolCallStatus _status = ToolCallStatus.None;
		public ToolCallStatus Status
		{
			get => _status;
			set => SetProperty(ref _status, value);
		}

		private string _toolName = string.Empty;
		public string ToolName
		{
			get => _toolName;
			set => SetProperty(ref _toolName, value);
		}

		private string _toolCallId = string.Empty;
		public string ToolCallId
		{
			get => _toolCallId;
			set => SetProperty(ref _toolCallId, value);
		}

		private string _arguments = string.Empty;
		public string Arguments
		{
			get => _arguments;
			set => SetProperty(ref _arguments, value);
		}

		private string _result = string.Empty;
		public string Result
		{
			get => _result;
			set => SetProperty(ref _result, value);
		}
	}
}