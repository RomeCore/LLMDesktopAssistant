using System.Collections.Immutable;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Agents;
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
		Chat chat,
		IAgentManagementService agentManager
	) : IToolExecutionService
	{
		public async Task ExecuteAsync(PartialFunctionToolCall? partialFunctionToolCall,
			AssistantMessage message, ToolCall toolCall, ToolInfo? toolInfo, CancellationToken cancellationToken = default)
		{
			object? sharedContext = null;

			if (partialFunctionToolCall != null)
			{
				toolCall.Status = ToolStatus.Pending;
				Func<JsonNode, ToolExecutionContext, StreamingToolArgumentsAnalysisResult>?
					streamingArgumentsAnalyser = toolInfo?.StreamingArgumentsAnalyser;

				var streamingToolExecutionContext = new ToolExecutionContext
				{
					Call = toolCall,
					Chat = chat,
					Info = toolInfo!,
					Message = message,
					SharedContext = sharedContext,
					RunningInUI = true,
					PolicyDecision = ToolPolicyDecision.None
				};

				void AddedPartialArg(object? sender, string deltaArg)
				{
					toolCall.Arguments = partialFunctionToolCall.Args;

					if (streamingArgumentsAnalyser != null)
					{
						try
						{
							// TolerantJsonParser can parse partial (unfinished) JSON too!
							var args = TolerantJsonParser.Parse(toolCall.Arguments);
							var analysisResult = streamingArgumentsAnalyser.Invoke(args ?? new JsonObject(),
								streamingToolExecutionContext);

							if (analysisResult.StopAnalysis)
								streamingArgumentsAnalyser = null;

							toolCall.StatusIcon = analysisResult.StatusIcon;
							toolCall.StatusTitle = analysisResult.StatusTitle;
						}
						catch (Exception ex)
						{
							Log.Debug(ex, "Error analyzing arguments: {ErrorMessage}", ex.Message);
						}
					}
				}

				partialFunctionToolCall.ArgsPartAdded += AddedPartialArg;
				try
				{
					await partialFunctionToolCall;
				}
				finally
				{
					partialFunctionToolCall.ArgsPartAdded -= AddedPartialArg;
					toolCall.StatusIcon = null;
					toolCall.StatusTitle = null;
				}

				sharedContext = streamingToolExecutionContext.SharedContext;
			}

			if (toolInfo == null)
			{
				toolCall.ResultContent = $"Error: Tool '{toolCall.ToolName}' not found.";
				toolCall.Status = ToolStatus.Error;
				return;
			}

			try
			{
				cancellationToken.ThrowIfCancellationRequested();

				JsonNode? parsedArgs = null;
				toolCall.ExpectedBehaviour = toolInfo.DefaultExpectedBehaviour;
				var toolHandledDecisions = toolInfo.DefaultSelfHandledDecisions;

				if (toolInfo.PreviewExecutor != null)
				{
					try
					{
						var previewToolExecutionContext = new ToolExecutionContext
						{
							Chat = chat,
							Message = message,
							Call = toolCall,
							Info = toolInfo,
							SharedContext = sharedContext,
							RunningInUI = true,
							PolicyDecision = ToolPolicyDecision.None
						};

						parsedArgs = TolerantJsonParser.Parse(toolCall.Arguments) ?? throw new InvalidOperationException("Invalid JSON format for tool arguments.");
						toolCall.Status = ToolStatus.PreExecuting;
						var preExecutionResult = await toolInfo.PreviewExecutor(parsedArgs, previewToolExecutionContext, cancellationToken);
						sharedContext = previewToolExecutionContext.SharedContext;
						toolCall.StatusTitle = preExecutionResult.StatusTitle;
						toolCall.StatusIcon = preExecutionResult.StatusIcon;

						if (preExecutionResult.InterruptingSuccess != null)
						{
							toolCall.Status = preExecutionResult.InterruptingSuccess.Value ? ToolStatus.Success : ToolStatus.Error;
							if (!string.IsNullOrEmpty(preExecutionResult.InterruptingContent))
								toolCall.ResultContent = preExecutionResult.InterruptingContent;
							else
							{
								if (preExecutionResult.InterruptingSuccess.Value)
									toolCall.ResultContent = "Tool successfully returned no result.";
								else
									toolCall.ResultContent = "Tool failed with no result.";
							}
							return;
						}

						if (preExecutionResult.ExpectedBehaviour != null)
							toolCall.ExpectedBehaviour = preExecutionResult.ExpectedBehaviour.Value;
						if (preExecutionResult.SelfHandledDecisions != null)
							toolHandledDecisions = preExecutionResult.SelfHandledDecisions.Value;
					}
					catch (Exception ex)
					{
						Log.Debug(ex, "Error during preview execution of tool '{ToolName}': {ExceptionMessage}", toolCall.ToolName, ex.Message);
					}
				}

				cancellationToken.ThrowIfCancellationRequested();

				var approvalLevel = toolInfo.ApprovalLevel;
				bool requireConfirmation, disallow = false;
				switch (approvalLevel)
				{
					case ToolApprovalLevel.AlwaysApprove:
						requireConfirmation = false;
						break;

					case ToolApprovalLevel.AlwaysAsk:
						requireConfirmation = true;
						break;

					case ToolApprovalLevel.AlwaysDisallow:
						if (toolHandledDecisions.HasFlag(ToolPolicyDecision.Disallow))
						{
							disallow = true;
							requireConfirmation = false;
							break;
						}
						toolCall.Status = ToolStatus.Error;
						toolCall.ResultContent = $"The tool execution is disallowed by the agent's settings policy.";
						return;

					default:
						throw new InvalidOperationException($"Invalid approval level for a tool: {approvalLevel}");

					case ToolApprovalLevel.PolicyBased:
					case ToolApprovalLevel.PolicyApproveOrAsk:
					case ToolApprovalLevel.PolicyApproveOrDisallow:
					case ToolApprovalLevel.PolicyAskOrDisallow:

						var senderAgent = agentManager.GetAgentDescriptor(message.SenderAgentId);
						var agentToolSettings = senderAgent.Tools;

						ToolBehaviour autoApproveBehaviours, disallowedBehaviours;

						if (agentToolSettings.EnablePolicyOverride)
						{
							autoApproveBehaviours = agentToolSettings.AutoApproveBehaviours;
							disallowedBehaviours = agentToolSettings.DisallowedBehaviours;
						}
						else
						{
							autoApproveBehaviours = chat.Settings.Tools.AutoApproveBehaviours;
							disallowedBehaviours = chat.Settings.Tools.DisallowedBehaviours;
						}

						disallow = (disallowedBehaviours & toolCall.ExpectedBehaviour) != 0;
						bool autoApprove = (autoApproveBehaviours & toolCall.ExpectedBehaviour) == toolCall.ExpectedBehaviour;
						
						switch (approvalLevel)
						{
							case ToolApprovalLevel.PolicyApproveOrAsk:
								disallow = false;
								break;

							case ToolApprovalLevel.PolicyApproveOrDisallow:
								autoApprove = true;
								break;

							case ToolApprovalLevel.PolicyAskOrDisallow:
								autoApprove = false;
								break;
						}

						if (disallow)
						{
							if (toolHandledDecisions.HasFlag(ToolPolicyDecision.Disallow))
							{
								requireConfirmation = !autoApprove;
								break;
							}
							toolCall.Status = ToolStatus.Error;
							toolCall.ResultContent = $"The tool execution is disallowed by the agent's settings policy. " +
								$"Policy disallows: {disallowedBehaviours}; the tool expected behaviour: {toolCall.ExpectedBehaviour}.";
							return;
						}

						requireConfirmation = !autoApprove;
						break;
				}

				if (requireConfirmation && !toolHandledDecisions.HasFlag(ToolPolicyDecision.Ask))
				{
					var tcs = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
					toolCall.ExpectedBehaviour ??= toolInfo.DefaultExpectedBehaviour;
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
					parsedArgs ??= TolerantJsonParser.Parse(toolCall.Arguments) ?? throw new InvalidOperationException("Invalid JSON format for tool arguments.");
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Error parsing tool arguments. Arguments: {Args}.", toolCall.Arguments);
					throw;
				}

				cancellationToken.ThrowIfCancellationRequested();

				toolCall.StatusIcon = null;
				toolCall.StatusTitle = null;
				var toolExecutionContext = new ToolExecutionContext
				{
					Chat = chat,
					Message = message,
					Call = toolCall,
					Info = toolInfo,
					SharedContext = sharedContext,
					RunningInUI = true,
					PolicyDecision = requireConfirmation ? ToolPolicyDecision.Ask :
									 disallow ? ToolPolicyDecision.Disallow :
									 ToolPolicyDecision.None
				};
				var reactiveResult = await toolInfo.Executor.Invoke(parsedArgs, toolExecutionContext, cancellationToken);

				toolCall.ReactiveToolResult = reactiveResult;
				toolCall.StatusIcon = reactiveResult.StatusIcon;
				toolCall.StatusTitle = reactiveResult.StatusTitle;
				toolCall.StructuredResult = reactiveResult.StructuredResult;
				toolCall.UseMarkdown = reactiveResult.UseMarkdown;
				toolCall.ResultContent = reactiveResult.ResultContent;

				void OnReactiveResultChanged(object? sender, object? e)
				{
					toolCall.StatusIcon = reactiveResult.StatusIcon;
					toolCall.StatusTitle = reactiveResult.StatusTitle;
					toolCall.StructuredResult = reactiveResult.StructuredResult;
					toolCall.UseMarkdown = reactiveResult.UseMarkdown;
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
					// Update again, because tool can be TOO FAST
					toolCall.StatusIcon = reactiveResult.StatusIcon;
					toolCall.StatusTitle = reactiveResult.StatusTitle;
					toolCall.StructuredResult = reactiveResult.StructuredResult;
					toolCall.ResultContent = reactiveResult.ResultContent;
					toolCall.UseMarkdown = reactiveResult.UseMarkdown;

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