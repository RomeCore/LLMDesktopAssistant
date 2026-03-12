using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.MVVM;

namespace LLMDesktopAssistant.LLM.MVVM
{
	public enum ToolCallStatus
	{
		None,
		UserAsked,
		InProgress,
		Success,
		Cancelled,
		Error,
		NoResult
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

		public ICommand ApproveCommand { get; }
		public void Approve()
		{
			Status = ToolCallStatus.InProgress;
			_userDecisionTcs?.TrySetResult(true);
		}

		public ICommand CancelCommand { get; }
		public void Cancel()
		{
			Status = ToolCallStatus.Cancelled;
			_userDecisionTcs?.TrySetResult(false);
		}

		public ToolCallViewModel()
		{
			ApproveCommand = new RelayCommand(Approve);
			CancelCommand = new RelayCommand(Cancel);
		}

		private TaskCompletionSource<bool>? _userDecisionTcs;
		public async Task<bool> AskUserAsync(CancellationToken cancellationToken)
		{
			if (_userDecisionTcs != null)
				throw new InvalidOperationException("A user decision is already in progress.");

			_userDecisionTcs = new TaskCompletionSource<bool>();
			Status = ToolCallStatus.UserAsked;

			using (cancellationToken.Register(() =>
			{
				_userDecisionTcs.TrySetCanceled(cancellationToken);
			}))
			{
				try
				{
					return await _userDecisionTcs.Task;
				}
				finally
				{
					_userDecisionTcs = null;
				}
			}
		}
	}
}