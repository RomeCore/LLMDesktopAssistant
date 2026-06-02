using System.Collections.Immutable;
using System.ComponentModel;
using System.Net.NetworkInformation;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Tools;
using Serilog;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
	[ChatService(typeof(IToolExecutionService))]
	public class ToolExecutionService(
		Chat chat
	) : IToolExecutionService
	{
		public async Task ExecuteAsync(AssistantMessage message, ToolCall toolCall, LLMInfo llmInfo,
			ImmutableDictionary<string, ToolInfo> tools, CancellationToken cancellationToken = default)
		{
			if (!tools.TryGetValue(toolCall.ToolName, out var toolInfo))
			{
				toolCall.ResultContent = $"Error: Tool '{toolCall.ToolName}' not found.";
				toolCall.Status = ToolStatus.Error;
				return;
			}

			try
			{
				JsonNode? parsedArgs = null;
				var toolExecutionContext = new ToolExecutionContext
				{
					Chat = chat,
					Message = message,
					Call = toolCall,
					Info = toolInfo
				};

				if (toolInfo.AskForConfirmation)
				{
					if (toolInfo.PreviewExecutor != null)
					{
						try
						{
							parsedArgs = TolerantJsonParser.Parse(toolCall.Arguments) ?? throw new InvalidOperationException("Invalid JSON format for tool arguments.");
							toolCall.Status = ToolStatus.PreExecuting;
							var preExecutionResult = await toolInfo.PreviewExecutor(parsedArgs, toolExecutionContext, cancellationToken);
							toolCall.StatusTitle = preExecutionResult.StatusTitle;
							toolCall.StatusIcon = preExecutionResult.StatusIcon;
						}
						catch
						{
						}
					}

					var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
					toolCall.UserConfirmationSource = tcs;
					toolCall.Status = ToolStatus.WaitingForApproval;

					using var ctr = cancellationToken.Register(() =>
					{
						tcs.TrySetCanceled(cancellationToken);
					});

					string? confirmation = await tcs.Task;
					if (confirmation != null)
					{
						toolCall.Status = ToolStatus.Cancelled;
						if (string.IsNullOrWhiteSpace(confirmation))
							toolCall.ResultContent = "User has cancelled the tool execution without a reason. " +
								"Maybe it can be dangerous or unwanted to proceed. Please wait for user message for explanations.";
						else
							toolCall.ResultContent = $"User has cancelled the tool execution with a reason: {confirmation}.";
						return;
					}
				}

				toolCall.Status = ToolStatus.Executing;

				try
				{
					// Use the parser that can tolerate lots of errors and still produce a valid JSON object.
					parsedArgs ??= TolerantJsonParser.Parse(toolCall.Arguments) ?? throw new InvalidOperationException("Invalid JSON format for tool arguments.");
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Error parsing tool arguments. Arguments: {Args}.", toolCall.Arguments);
					throw;
				}
				var reactiveResult = await toolInfo.Executor.Invoke(parsedArgs, toolExecutionContext, cancellationToken);

				toolCall.ReactiveToolResult = reactiveResult;
				toolCall.StatusIcon = reactiveResult.StatusIcon;
				toolCall.StatusTitle = reactiveResult.StatusTitle;
				toolCall.StructuredResult = reactiveResult.StructuredResult;
				toolCall.ResultContent = reactiveResult.ResultContent;
				toolCall.UseMarkdown = reactiveResult.UseMarkdown;

				void OnReactiveResultChanged(object? sender, object? e)
				{
					toolCall.StatusIcon = reactiveResult.StatusIcon;
					toolCall.StatusTitle = reactiveResult.StatusTitle;
					toolCall.UseMarkdown = reactiveResult.UseMarkdown;
					toolCall.StructuredResult = reactiveResult.StructuredResult;
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

				toolCall.ResultContent = reactiveResult.ResultContent;
				toolCall.Status = cancellationToken.IsCancellationRequested ? ToolStatus.Cancelled :
					(success ? ToolStatus.Success : ToolStatus.Error);

				if (string.IsNullOrEmpty(toolCall.ResultContent))
				{
					switch (toolCall.Status)
					{
						default:
							if (success)
								toolCall.ResultContent = "Tool successfully returned no result.";
							else
								toolCall.ResultContent = "Tool failed with no result.";
							break;
						case ToolStatus.Cancelled:
							toolCall.ResultContent = "Tool execution was cancelled.";
							break;
					}
				}

				return;
			}
			catch (AggregateException aex) when (aex.InnerExceptions.Any(e => e is OperationCanceledException))
			{
				toolCall.Status = ToolStatus.Cancelled;
				if (string.IsNullOrEmpty(toolCall.ResultContent))
					toolCall.ResultContent = "Tool execution was cancelled.";
				else
					toolCall.ResultContent += "\nTool execution was interrupted.";
			}
			catch (OperationCanceledException)
			{
				toolCall.Status = ToolStatus.Cancelled;
				if (string.IsNullOrEmpty(toolCall.ResultContent))
					toolCall.ResultContent = "Tool execution was cancelled.";
				else
					toolCall.ResultContent += "\nTool execution was interrupted.";
			}
			catch (Exception ex)
			{
				toolCall.Status = ToolStatus.Error;
				if (string.IsNullOrEmpty(toolCall.ResultContent))
					toolCall.ResultContent = "Tool execution failed with error: " + ex.Message;
				else
					toolCall.ResultContent += "\nTool execution was interrupted with error: " + ex.Message;
			}
		}
	}
}