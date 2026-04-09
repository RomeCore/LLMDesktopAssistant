using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.ToolModules;
using Microsoft.Extensions.DependencyInjection;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Tools;
using System.Collections.Immutable;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
	public class ToolExecutionService(IServiceProvider services) : IToolExecutionService
	{
		readonly IToolExecutionHook? hook = services.GetService<IToolExecutionHook>();

		public async Task ExecuteAsync(ToolCall toolCall, LLMInfo llmInfo, ImmutableDictionary<string, ToolInfo> tools, CancellationToken cancellationToken = default)
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
				throw new InvalidOperationException($"Tool '{toolCall.ToolName}' not found.");
			if (toolInfo.Tool is not FunctionTool functionTool)
				throw new InvalidOperationException($"Tool '{toolCall.ToolName}' is not a function tool.");

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
							"Maybe it can be dangerous to proceed. Please try again with different parameters.");
				}

				if (result == null)
				{
					toolCall.Status = ToolStatus.Executing;
					result = await functionTool.ExecuteAsync(toolCall.Arguments, cancellationToken);
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