using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input.Platform;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;
using Material.Icons;
using System.ComponentModel;
using System.Text.Json.Nodes;
using System.Windows.Input;

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

		private string _toolTitle = string.Empty;
		public string ToolTitle
		{
			get => _toolTitle;
			set => SetProperty(ref _toolTitle, value);
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
					RaisePropertyChanged(nameof(ToolIconBrush));
					RaisePropertyChanged(nameof(DisplayMiniIcon));
					RaisePropertyChanged(nameof(IsBlinking));
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



		private ToolDangerLevel _dangerLevel;
		public ToolDangerLevel DangerLevel
		{
			get => _dangerLevel;
			set => SetProperty(ref _dangerLevel, value);
		}

		private MaterialIconKind? _statusIcon;
		public MaterialIconKind? StatusIcon
		{
			get => _statusIcon;
			set => SetProperty(ref _statusIcon, value);
		}

		private string? _statusTitle;
		public string? StatusTitle
		{
			get => _statusTitle;
			set => SetProperty(ref _statusTitle, value);
		}

		private string? _result = string.Empty;
		public string? Result
		{
			get => _result;
			set => SetProperty(ref _result, value);
		}

		private bool _useMarkdown = false;
		public bool UseMarkdown
		{
			get => _useMarkdown;
			set => SetProperty(ref _useMarkdown, value);
		}
		


		public MaterialIconKind ToolIcon =>
			Status switch
			{
				ToolStatus.Pending => MaterialIconKind.Edit,
				ToolStatus.PreExecuting or ToolStatus.WaitingForApproval when DangerLevel is ToolDangerLevel.Safe
					=> MaterialIconKind.CheckCircle,
				ToolStatus.PreExecuting or ToolStatus.WaitingForApproval when DangerLevel is ToolDangerLevel.Warning
					=> MaterialIconKind.WarningCircle,
				ToolStatus.PreExecuting or ToolStatus.WaitingForApproval when DangerLevel is ToolDangerLevel.Dangerous
					=> MaterialIconKind.AlertCircle,
				ToolStatus.PreExecuting => MaterialIconKind.WrenchClock,
				ToolStatus.Executing => MaterialIconKind.WrenchClock,
				ToolStatus.WaitingForApproval => MaterialIconKind.QuestionMarkCircle,
				ToolStatus.ExecutionInterrupted => MaterialIconKind.CancelCircle,
				ToolStatus.Success => MaterialIconKind.CheckCircle,
				ToolStatus.Cancelled => MaterialIconKind.CancelCircle,
				ToolStatus.Error => MaterialIconKind.AlertCircle,
				_ => MaterialIconKind.Wrench
			};

		public IBrush? ToolIconBrush =>
			Status switch
			{
				ToolStatus.Pending => null,
				ToolStatus.PreExecuting or ToolStatus.WaitingForApproval when DangerLevel is ToolDangerLevel.Safe
					=> Brushes.Green,
				ToolStatus.PreExecuting or ToolStatus.WaitingForApproval when DangerLevel is ToolDangerLevel.Warning
					=> Brushes.Yellow,
				ToolStatus.PreExecuting or ToolStatus.WaitingForApproval when DangerLevel is ToolDangerLevel.Dangerous
					=> Brushes.Red,
				ToolStatus.PreExecuting => null,
				ToolStatus.Executing => null,
				ToolStatus.WaitingForApproval => null,
				ToolStatus.ExecutionInterrupted => null,
				ToolStatus.Success => null,
				ToolStatus.Cancelled => null,
				ToolStatus.Error => null,
				_ => null
			} ?? Brushes.White;

		public bool DisplayMiniIcon =>
			Status switch
			{
				ToolStatus.Success => false,
				_ => true
			};

		public bool IsBlinking =>
			Status switch
			{
				ToolStatus.Pending => true,
				_ => false
			};

		public bool InProgress =>
			Status switch
			{
				ToolStatus.PreExecuting => true,
				ToolStatus.Executing => true,
				_ => false
			};

		public bool ToolIconVisible =>
			Status switch
			{
				ToolStatus.PreExecuting => false,
				ToolStatus.Executing => false,
				_ => true
			};

		public bool UserAsked =>
			Status switch
			{
				ToolStatus.WaitingForApproval => true,
				_ => false
			} && !WritingReason;

		private bool _writingReason = false;
		public bool WritingReason
		{
			get => _writingReason;
			set
			{
				if (SetProperty(ref _writingReason, value))
				{
					RaisePropertyChanged(nameof(UserAsked));
				}
			}
		}



		public ICommand ApproveCommand { get; }
		public void Approve()
		{
			toolCall.UserConfirmationSource?.TrySetResult(null);
		}

		public ICommand CancelCommand { get; }
		public void Cancel()
		{
			toolCall.UserConfirmationSource?.TrySetResult(string.Empty);
		}

		public ICommand CancelWithReasonBeginCommand { get; }
		public ICommand CancelWithReasonEndCommand { get; }
		public ICommand CancelWithReasonBackCommand { get; }
		public void CancelWithReason()
		{
			WritingReason = true;
		}
		public void CancelWithReason(string? reason)
		{
			WritingReason = false;
			toolCall.UserConfirmationSource?.TrySetResult(reason ?? string.Empty);
		}
		public void CancelWithReasonBack()
		{
			WritingReason = false;
		}

		public ICommand CopyArgumentsCommand { get; }
		public void CopyArguments()
		{
			if (!string.IsNullOrEmpty(toolCall.Arguments))
			{
				App.MainTopLevel.Clipboard?.SetTextAsync(toolCall.Arguments);
			}
		}

		public ICommand CopyResultCommand { get; }
		public void CopyResult()
		{
			if (!string.IsNullOrEmpty(Result))
			{
				App.MainTopLevel.Clipboard?.SetTextAsync(Result);
			}
		}



		public ToolCallViewModel(ToolCall toolCall, Chat chat)
		{
			this.toolCall = toolCall;

			ToolName = toolCall.ToolName;
			ToolTitle = toolCall.Title ?? toolCall.ToolName;
			ToolCallId = toolCall.Id;
			try
			{
				var parsedArgs = TolerantJsonParser.Parse(toolCall.Arguments);
				Arguments = ToolCallArgumentFormatter.FormatToMarkdown(parsedArgs);
			}
			catch
			{
				Arguments = "```json\n" + toolCall.Arguments + "\n```";
			}

			Status = toolCall.Status;
			Progress = toolCall.ReactiveToolResult?.Progress;
			MinProgress = toolCall.ReactiveToolResult?.MinProgress ?? 0.0;
			MaxProgress = toolCall.ReactiveToolResult?.MaxProgress ?? 1.0;

			StatusIcon = toolCall.StatusIcon;
			StatusTitle = toolCall.StatusTitle;
			DangerLevel = toolCall.DangerLevel;

			Result = toolCall.ResultContent;
			UseMarkdown = toolCall.UseMarkdown;

			ApproveCommand = new RelayCommand(Approve);
			CancelCommand = new RelayCommand(Cancel);
			CancelWithReasonBeginCommand = new RelayCommand(CancelWithReason);
			CancelWithReasonEndCommand = new RelayCommand<string?>(CancelWithReason);
			CancelWithReasonBackCommand = new RelayCommand(CancelWithReasonBack);
			CopyArgumentsCommand = new RelayCommand(CopyArguments);
			CopyResultCommand = new RelayCommand(CopyResult);

			if (!toolCall.IsCompleted)
			{
				var reactiveToolResult = toolCall.ReactiveToolResult;
				void OnToolCallPropertyChanged(object? s, PropertyChangedEventArgs e)
				{
					InvokeUI(() =>
					{
						switch (e.PropertyName)
						{
							case nameof(ToolCall.Arguments):
								try
								{
									var parsedArgs = TolerantJsonParser.Parse(toolCall.Arguments);
									Arguments = ToolCallArgumentFormatter.FormatToMarkdown(parsedArgs);
								}
								catch
								{
									Arguments = "```json\n" + toolCall.Arguments + "\n```";
								}
								break;

							case nameof(ToolCall.Status): Status = toolCall.Status; break;
							case nameof(ToolCall.StatusIcon): StatusIcon = toolCall.StatusIcon; break;
							case nameof(ToolCall.StatusTitle): StatusTitle = toolCall.StatusTitle; break;
							case nameof(ToolCall.DangerLevel): DangerLevel = toolCall.DangerLevel; break;
							case nameof(ToolCall.ResultContent): Result = toolCall.ResultContent; break;
							case nameof(ToolCall.UseMarkdown): UseMarkdown = toolCall.UseMarkdown; break;

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

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposing)
			{
				
			}
		}
	}
}
