using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Tools;
using Material.Icons;
using System.ComponentModel;

namespace LLMDesktopAssistant.LLM.Messages
{
	[ViewModelFor(typeof(ToolCallView))]
	public class ToolCallViewModel : ViewModelBase
	{
		private readonly ToolCall toolCall;

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



		private ToolStatus _status = ToolStatus.None;
		public ToolStatus Status
		{
			get => _status;
			set
			{
				if (SetProperty(ref _status, value))
				{
					RaisePropertyChanged(nameof(ToolIcon));
					RaisePropertyChanged(nameof(IsPending));
					RaisePropertyChanged(nameof(InProgress));
					RaisePropertyChanged(nameof(ToolIconVisible));
					RaisePropertyChanged(nameof(UserAsked));
				}
			}
		}

		private double? _progress = null;
		public double? Progress
		{
			get => _progress;
			set => SetProperty(ref _progress, value);
		}

		private double _minProgress = 0.0;
		public double MinProgress
		{
			get => _minProgress;
			set => SetProperty(ref _minProgress, value);
		}

		private double _maxProgress = 1.0;
		public double MaxProgress
		{
			get => _maxProgress;
			set => SetProperty(ref _maxProgress, value);
		}



		private MaterialIconKind? _statusIcon;
		public MaterialIconKind? StatusIcon
		{
			get => _statusIcon;
			set
			{
				if (SetProperty(ref _statusIcon, value))
				{
					RaisePropertyChanged(nameof(StatusAvailable));
				}
			}
		}

		private string? _statusTitle;
		public string? StatusTitle
		{
			get => _statusTitle;
			set
			{
				if (SetProperty(ref _statusTitle, value))
				{
					RaisePropertyChanged(nameof(StatusAvailable));
				}
			}
		}

		public bool StatusAvailable => StatusIcon.HasValue || !string.IsNullOrEmpty(StatusTitle);


		private string? _result = string.Empty;
		public string? Result
		{
			get => _result;
			set => SetProperty(ref _result, value);
		}



		public MaterialIconKind ToolIcon =>
			Status switch
			{
				ToolStatus.WaitingForApproval => MaterialIconKind.QuestionMarkCircle,
				ToolStatus.ExecutionInterrupted => MaterialIconKind.CancelCircle,
				ToolStatus.Success => MaterialIconKind.CheckCircle,
				ToolStatus.Cancelled => MaterialIconKind.CancelCircle,
				ToolStatus.Error => MaterialIconKind.AlertCircle,
				_ => MaterialIconKind.Hammer
			};

		public bool IsPending =>
			Status switch
			{
				ToolStatus.Pending => true,
				_ => false
			};

		public bool InProgress =>
			Status switch
			{
				ToolStatus.Executing => true,
				_ => false
			};

		public bool ToolIconVisible =>
			Status switch
			{
				ToolStatus.Pending => false,
				ToolStatus.Executing => false,
				_ => true
			};

		public bool UserAsked =>
			Status switch
			{
				ToolStatus.WaitingForApproval => true,
				_ => false
			};



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



		public ToolCallViewModel(ToolCall toolCall, Chat chat)
		{
			this.toolCall = toolCall;

			ToolName = toolCall.Title ?? toolCall.ToolName;
			ToolCallId = toolCall.Id;
			Arguments = ToolCallArgumentFormatter.FormatToMarkdown(toolCall.Arguments);

			Status = toolCall.Status;
			Progress = toolCall.ReactiveToolResult?.Progress;
			MinProgress = toolCall.ReactiveToolResult?.MinProgress ?? 0.0;
			MaxProgress = toolCall.ReactiveToolResult?.MaxProgress ?? 1.0;

			StatusIcon = toolCall.StatusIcon;
			StatusTitle = toolCall.StatusTitle;

			Result = toolCall.ResultContent;

			ApproveCommand = new RelayCommand(Approve);
			CancelCommand = new RelayCommand(Cancel);

			if (!toolCall.IsCompleted)
			{
				var reactiveToolResult = toolCall.ReactiveToolResult;
				void OnToolCallPropertyChanged(object? s, PropertyChangedEventArgs e)
				{
					InvokeUI(() =>
					{
						Status = toolCall.Status;
						StatusIcon = toolCall.StatusIcon;
						StatusTitle = toolCall.StatusTitle;
						Result = toolCall.ResultContent;

						switch (e.PropertyName)
						{
							case nameof(ToolCall.Status): Status = toolCall.Status; break;
							case nameof(ToolCall.StatusIcon): StatusIcon = toolCall.StatusIcon; break;
							case nameof(ToolCall.StatusTitle): StatusTitle = toolCall.StatusTitle; break;
							case nameof(ToolCall.ResultContent): Result = toolCall.ResultContent; break;

							case nameof(ToolCall.ReactiveToolResult):
								reactiveToolResult?.PropertyChanged -= OnReactiveToolResultPropertyChanged;
								reactiveToolResult = toolCall.ReactiveToolResult;
								reactiveToolResult?.PropertyChanged += OnReactiveToolResultPropertyChanged;
								OnReactiveToolResultPropertyChanged(null, null!);
								break;
						}
					});
				}
				void OnReactiveToolResultPropertyChanged(object? s, PropertyChangedEventArgs e)
				{
					InvokeUI(() =>
					{
						Progress = reactiveToolResult?.Progress;
						MinProgress = reactiveToolResult?.MinProgress ?? 0.0;
						MaxProgress = reactiveToolResult?.MaxProgress ?? 1.0;
					});
				}

				toolCall.PropertyChanged += OnToolCallPropertyChanged;
				reactiveToolResult?.PropertyChanged += OnReactiveToolResultPropertyChanged;
				toolCall.CompletionToken.OnCompleted(() =>
				{
					toolCall.PropertyChanged -= OnToolCallPropertyChanged;
					reactiveToolResult?.PropertyChanged -= OnReactiveToolResultPropertyChanged;
				});
			}
		}
	}
}