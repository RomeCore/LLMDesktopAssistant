using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Tools;
using Microsoft.Extensions.DependencyInjection;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Tools;
using System.Collections.Immutable;
using System.ComponentModel;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
	[ChatService(typeof(IToolExecutionService))]
	public class ToolExecutionService(
		Chat chat,
		IServiceProvider services
	) : IToolExecutionService
	{
		readonly IToolExecutionHook? hook = services.GetService<IToolExecutionHook>();

		public async Task ExecuteAsync(AssistantMessage message, ToolCall toolCall, LLMInfo llmInfo,
			ImmutableDictionary<string, ToolInfo> tools, CancellationToken cancellationToken = default)
		{
			ToolResult? result = null;

			if (hook != null)
			{
				result = await hook.OnBeforeExecuteAsync(toolCall, llmInfo, cancellationToken);

				if (result != null)
				{
					toolCall.ResultContent = result.Content;
					toolCall.Status = result.Status switch
					{
						ToolResultStatus.Success => ToolStatus.Success,
						ToolResultStatus.Error => ToolStatus.Error,
						ToolResultStatus.Cancelled => ToolStatus.Cancelled,
						ToolResultStatus.NoResult => ToolStatus.NoResult,
						_ => ToolStatus.NoResult
					};

					return;
				}
			}

			if (!tools.TryGetValue(toolCall.ToolName, out var toolInfo))
			{
				toolCall.ResultContent = $"Error: Tool '{toolCall.ToolName}' not found.";
				toolCall.Status = ToolStatus.Error;
				return;
			}

			try
			{
				if (toolInfo.AskForConfirmation)
				{
					var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
					toolCall.UserAskCompletionSource = tcs;
					toolCall.Status = ToolStatus.WaitingForApproval;

					using var ctr = cancellationToken.Register(() =>
					{
						tcs.TrySetCanceled(cancellationToken);
					});

					bool approved = await tcs.Task;
					if (!approved)
						result = new ToolResult(ToolResultStatus.Cancelled, "User has cancelled the tool execution. " +
							"Maybe it can be dangerous or unwanted to proceed. Please wait for user message for explanations.");
				}

				if (result == null)
				{
					toolCall.Status = ToolStatus.Executing;
					var toolExecutionContext = new ToolExecutionContext
					{
						Chat = chat,
						Message = message,
						Call = toolCall
					};
					var reactiveResult = await toolInfo.Executor.Invoke(toolCall.Arguments, toolExecutionContext, cancellationToken);

					toolCall.ReactiveToolResult = reactiveResult;
					toolCall.StatusIcon = reactiveResult.StatusIcon;
					toolCall.StatusTitle = reactiveResult.StatusTitle;
					toolCall.ResultContent = reactiveResult.ResultContent;

					void OnReactiveResultChanged(object? sender, object? e)
					{
						toolCall.StatusIcon = reactiveResult.StatusIcon;
						toolCall.StatusTitle = reactiveResult.StatusTitle;
					}
					void OnReactiveResultContentChanged(object? sender, object? e)
					{
						toolCall.ResultContent = reactiveResult.ResultContent;
					}
					reactiveResult.PropertyChanged += OnReactiveResultChanged;
					reactiveResult.ResultContentLines.CollectionChanged += OnReactiveResultContentChanged;

					bool success;
					try
					{
						success = await reactiveResult.Completion;
					}
					finally
					{
						toolCall.ReactiveToolResult = null;
						reactiveResult.PropertyChanged -= OnReactiveResultChanged;
						reactiveResult.ResultContentLines.CollectionChanged -= OnReactiveResultContentChanged;
					}

					var content = reactiveResult.ResultContent;
					ToolResultStatus status = cancellationToken.IsCancellationRequested ? ToolResultStatus.Cancelled :
						(string.IsNullOrWhiteSpace(content) ? ToolResultStatus.NoResult :
							(success ? ToolResultStatus.Success : ToolResultStatus.Error));

					switch (status)
					{
						case ToolResultStatus.NoResult:
							if (success)
								content = "Tool successfully returned no result.";
							else
								content = "Tool failed with no result.";
							break;
						case ToolResultStatus.Cancelled:
							content = "Tool execution was cancelled.";
							break;
					}

					result = new ToolResult(status, content);
				}
			}
			catch (AggregateException aex) when (aex.InnerExceptions.Any(e => e is OperationCanceledException))
			{
				result = new ToolResult(ToolResultStatus.Cancelled, "Tool execution was cancelled.");
			}
			catch (OperationCanceledException ocex)
			{
				string content = string.IsNullOrEmpty(ocex.Message) ? "Tool execution was cancelled." : ocex.Message;
				result = new ToolResult(ToolResultStatus.Cancelled, content);
			}
			catch (Exception ex)
			{
				result = new ToolResult(ToolResultStatus.Error, ex.Message);
			}

			toolCall.ResultContent = result.Content;
			toolCall.Status = result.Status switch
			{
				ToolResultStatus.Success => ToolStatus.Success,
				ToolResultStatus.Error => ToolStatus.Error,
				ToolResultStatus.Cancelled => ToolStatus.Cancelled,
				ToolResultStatus.NoResult => ToolStatus.NoResult,
				_ => ToolStatus.NoResult
			};
		}
	}
}