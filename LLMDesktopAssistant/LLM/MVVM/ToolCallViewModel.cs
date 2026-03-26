using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.MVVM;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;

namespace LLMDesktopAssistant.LLM.MVVM
{
	[ViewModelFor(typeof(ToolCallView))]
	public class ToolCallViewModel : ViewModelBase
	{
		private readonly ToolCall toolCall;

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

		private string? _result = string.Empty;
		public string? Result
		{
			get => _result;
			set => SetProperty(ref _result, value);
		}

		public ICommand ApproveCommand { get; }
		public void Approve()
		{
			toolCall.UserAskCompletionSource?.TrySetResult(true);
		}

		public ICommand CancelCommand { get; }
		public void Cancel()
		{
			toolCall.UserAskCompletionSource?.TrySetResult(false);
		}

		public ToolCallViewModel(ToolCall toolCall)
		{
			this.toolCall = toolCall;

			ApproveCommand = new RelayCommand(Approve);
			CancelCommand = new RelayCommand(Cancel);

			ToolName = toolCall.ToolName;
			ToolCallId = toolCall.Id;
			Arguments = ToolCallArgumentFormatter.FormatToMarkdown(toolCall.Arguments);
			Status = toolCall.Status switch
			{
				ToolStatus.NotExecuted => ToolCallStatus.None,
				ToolStatus.Executing => ToolCallStatus.InProgress,
				ToolStatus.WaitingForApproval => ToolCallStatus.UserAsked,
				ToolStatus.Success => ToolCallStatus.Success,
				ToolStatus.Error => ToolCallStatus.Error,
				ToolStatus.Cancelled => ToolCallStatus.Cancelled,
				_ => ToolCallStatus.None
			};
			Result = toolCall.ResultContent;

			if (!toolCall.IsCompleted)
			{
				void OnToolCallPropertyChanged(object? s, PropertyChangedEventArgs e)
				{
					InvokeUI(() =>
					{
						Status = toolCall.Status switch
						{
							ToolStatus.NotExecuted => ToolCallStatus.None,
							ToolStatus.Executing => ToolCallStatus.InProgress,
							ToolStatus.WaitingForApproval => ToolCallStatus.UserAsked,
							ToolStatus.Success => ToolCallStatus.Success,
							ToolStatus.Error => ToolCallStatus.Error,
							ToolStatus.Cancelled => ToolCallStatus.Cancelled,
							_ => ToolCallStatus.None
						};
						Result = toolCall.ResultContent;
					});
				}
				toolCall.PropertyChanged += OnToolCallPropertyChanged;
				toolCall.CompletionToken.OnCompleted(() => toolCall.PropertyChanged -= OnToolCallPropertyChanged);
			}
		}
	}
}