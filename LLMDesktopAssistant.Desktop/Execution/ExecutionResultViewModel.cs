using LLMDesktopAssistant.MVVM;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Desktop.Execution
{
	[ViewModelFor(typeof(ExecutionResultView))]
	public class ExecutionResultViewModel : ViewModelBase
	{
		private string _outputText = string.Empty;
		public string OutputText
		{
			get => _outputText;
			set => SetProperty(ref _outputText, value);
		}

		private bool _isError = false;
		public bool IsError
		{
			get => _isError;
			set => SetProperty(ref _isError, value);
		}
	}
}