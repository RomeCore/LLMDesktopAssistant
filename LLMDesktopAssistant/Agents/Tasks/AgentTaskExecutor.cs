using System.Diagnostics;
using System.Text.Json.Nodes;
using LLMDesktopAssistant.Data;
using LLMDesktopAssistant.Providers;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Utils;
using RCLargeLanguageModels;
using RCLargeLanguageModels.Completions;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Messages.Attachments;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Agents.Tasks
{
	[Service(typeof(IAgentTaskExecutor))]
	public class AgentTaskExecutor : IAgentTaskExecutor
	{
		private readonly AsyncLocal<AgentTask?> _currentTask = new();
		private readonly RangeObservableCollection<AgentTask> _allTasks;
		private readonly IModelManager _modelManager;
		private readonly IToolApprovalService _toolApprovalService;
		private readonly IUsageStatsCollector _usageStatsCollector;

		public ReadOnlyObservableCollection<AgentTask> AllTasks { get; }

		public AgentTaskExecutor(IModelManager modelManager, IToolApprovalService toolApprovalService,
			IUsageStatsCollector usageStatsCollector)
		{
			_allTasks = [];
			_modelManager = modelManager;
			_toolApprovalService = toolApprovalService;
			_usageStatsCollector = usageStatsCollector;

			AllTasks = new ReadOnlyObservableCollection<AgentTask>(_allTasks);
		}

		public AgentTask Execute(AgentTaskLaunchParameters parameters, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(parameters.ModelName) && parameters.Model == null)
				throw new ArgumentException("Model name or model must be provided.");
			if (parameters.InitialMessages.IsEmpty)
				throw new ArgumentException("Initial messages must be provided.", nameof(parameters.InitialMessages));
			if (parameters.MaxParallelToolCalls < 1)
				throw new ArgumentException("Max parallel tool calls must be greater than 0.", nameof(parameters.MaxParallelToolCalls));
			if (!Enum.IsDefined(parameters.Behaviour))
				throw new ArgumentException("Invalid behaviour value.", nameof(parameters.Behaviour));

			var completionSource = new TaskCompletionSource<AgentTask>();

			CancellationToken timeoutCt = default;
			if (parameters.TimeOut.HasValue && parameters.TimeOut.Value > TimeSpan.Zero)
				timeoutCt = new CancellationTokenSource(parameters.TimeOut.Value).Token;
			var task = new AgentTask
			{
				Completion = completionSource.Task,
				CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCt),
				LaunchParameters = parameters
			};
			task.Messages.AddRange(parameters.InitialMessages);

			var model = parameters.Model ?? _modelManager.GetModel(parameters.ModelName!);

			model = model.WithTools(parameters.Tools.Select(t => new FunctionTool(t.Name, t.Description, t.ArgumentSchema,
				(_, _) => throw new Exception("This tool is not expected to be invoked directly."))));

			var tools = parameters.Tools.ToImmutableDictionary(k => k.Name);

			var nativeMessages = new List<IMessage>();
			foreach (var message in task.Messages)
				nativeMessages.AddRange(ConvertMessageFromAgent(message));

			var parentTask = _currentTask.Value;

			Task.Run(async () =>
			{
				task.Status = AgentTaskStatus.Executing;
				_allTasks.Add(task);
				parentTask?.SubTasks.Add(task);
				parameters.TriggeredChat?.AgentTasks.Add(task);
				parameters.TriggeredMessage?.AgentTasks.Add(task);
				_currentTask.Value = task;

				try
				{
					await ExecuteAsync(task, model, tools, nativeMessages);
					task.Status = AgentTaskStatus.Success;
					completionSource.SetResult(task);
				}
				catch (AggregateException aex) when (aex.InnerExceptions.Any(e => e is OperationCanceledException))
				{
					task.Status = AgentTaskStatus.Cancelled;
					completionSource.SetCanceled(cancellationToken);
				}
				catch (OperationCanceledException)
				{
					task.Status = AgentTaskStatus.Cancelled;
					completionSource.SetCanceled(cancellationToken);
				}
				catch (Exception ex)
				{
					task.Status = AgentTaskStatus.Failed;
					task.Exception = ex;
					completionSource.SetException(ex);
				}
				finally
				{
					task.Completed = true;

					if (parameters.CompletionExpiryTime != null)
					{
						if (parameters.CompletionExpiryTime.Value > TimeSpan.Zero)
							await Task.Delay(parameters.CompletionExpiryTime.Value);

						_allTasks.Remove(task);
						parentTask?.SubTasks.Remove(task);
						parameters.TriggeredChat?.AgentTasks.Remove(task);
						parameters.TriggeredMessage?.AgentTasks.Remove(task);
					}
				}
			}, CancellationToken.None);

			return task;
		}

		private async Task ExecuteAsync(AgentTask task, LLModel model,
			ImmutableDictionary<string, AgentTool> tools, List<IMessage> nativeMessages)
		{
			var cancellationToken = task.CancellationTokenSource.Token;
			var semaphore = new SemaphoreSlim(task.LaunchParameters.MaxParallelToolCalls, task.LaunchParameters.MaxParallelToolCalls);

			var executionTimer = new Stopwatch();
			executionTimer.Start();

			int totalInputTokens = 0, totalOutputTokens = 0;
			int totalCacheHitTokens = 0, totalCacheMissTokens = 0;
			TimeSpan totalTtft = TimeSpan.Zero, totalInferenceTime = TimeSpan.Zero;

			while (true)
			{
				Stopwatch messageExecutionTimer = new(), inferenceTimer = new();
				messageExecutionTimer.Start();
				inferenceTimer.Start();

				try
				{
					cancellationToken.ThrowIfCancellationRequested();

					TimeSpan? ttft = null;
					TimeSpan inferenceTime = TimeSpan.Zero;

					var response = await model.ChatStreamingAsync(nativeMessages, cancellationToken: cancellationToken);
					IAssistantMessage responseMessage = response.Message;
					nativeMessages.Add(responseMessage);

					var agentMessage = new AgentAssistantMessage
					{
						ReasoningContent = responseMessage.ReasoningContent,
						Content = responseMessage.Content
					};
					agentMessage.Attachments.AddRange(responseMessage.Attachments
						.Select(a => AgentAttachment.TryConvertFromNativeAttachment(a)).Where(a => a != null)!);

					async Task<IToolMessage> ProcessToolCall(IToolCall toolCall)
					{
						var toolExecutionTask = ExecuteToolAsync(toolCall, agentMessage, task, tools, cancellationToken);

						await semaphore.WaitAsync(cancellationToken);
						try
						{
							return await toolExecutionTask;
						}
						finally
						{
							semaphore.Release();
						}
					}
					List<Task<IToolMessage>> toolCallTasks = [];

					for (int i = 0; i < responseMessage.ToolCalls.Count; i++)
						toolCallTasks.Add(ProcessToolCall(responseMessage.ToolCalls[i]));

					task.Messages.Add(agentMessage);
					task.LastGeneratedMessage = agentMessage;
					task.LastGeneratedContent = agentMessage.Content;

					if (responseMessage is PartialAssistantMessage partialResponseMessage)
					{
						void PartAdded(object? sender, AssistantMessageDelta delta)
						{
							ttft ??= executionTimer.Elapsed;

							if (delta.DeltaReasoningContent != null)
								agentMessage.ReasoningContent = responseMessage.ReasoningContent;

							if (delta.DeltaContent != null)
							{
								agentMessage.Content = responseMessage.Content;
								task.LastGeneratedContent = responseMessage.Content;
							}

							if (delta.NewAttachments != null)
								agentMessage.Attachments.AddRange(delta.NewAttachments
									.Select(a => AgentAttachment.TryConvertFromNativeAttachment(a)).Where(a => a != null)!);

							if (delta.NewToolCalls != null)
								foreach (var toolCall in delta.NewToolCalls)
									toolCallTasks.Add(ProcessToolCall(toolCall));
						}
						partialResponseMessage.PartAdded += PartAdded;

						try
						{
							inferenceTimer.Restart();
							await partialResponseMessage;
							if (response is PartialChatCompletionResult partialResponse)
								await partialResponse;
							inferenceTime = inferenceTimer.Elapsed;
						}
						finally
						{
							partialResponseMessage.PartAdded -= PartAdded;
						}
					}

					ttft ??= TimeSpan.Zero;
					inferenceTime = inferenceTimer.Elapsed;
					totalTtft += ttft.Value;
					totalInferenceTime += inferenceTime;

					int inputTokens = 0, outputTokens = 0;
					int cacheHitTokens = 0, cacheMissTokens = 0;

					if (response.UsageMetadata is IUsageMetadata usageMetadata)
					{
						inputTokens = usageMetadata.InputTokens;
						outputTokens = usageMetadata.OutputTokens;

						if (usageMetadata is IUsageCacheMetadata cacheMetadata)
						{
							cacheHitTokens = cacheMetadata.InputCacheHitTokens;
							cacheMissTokens = cacheMetadata.InputCacheMissTokens;

							if (cacheHitTokens < 0) cacheHitTokens = 0;
							if (cacheMissTokens < 0) cacheMissTokens = 0;
						}

						if (inputTokens < 0) inputTokens = 0;
						if (outputTokens < 0) outputTokens = 0;
					}

					totalInputTokens += inputTokens;
					totalOutputTokens += outputTokens;
					totalCacheHitTokens += cacheHitTokens;
					totalCacheMissTokens += cacheMissTokens;

					agentMessage.UsageStatistics = new AgentUsageStatistics
					{
						InputTokens = inputTokens,
						OutputTokens = outputTokens,
						InputCacheHitTokens = cacheHitTokens,
						InputCacheMissTokens = cacheMissTokens,
						TimeToFirstToken = ttft.Value,
						InferenceTime = inferenceTime,
						ExecutionTime = messageExecutionTimer.Elapsed
					};

					task.UsageStatistics = new AgentUsageStatistics
					{
						InputTokens = totalInputTokens,
						OutputTokens = totalOutputTokens,
						InputCacheHitTokens = totalCacheHitTokens,
						InputCacheMissTokens = totalCacheMissTokens,
						TimeToFirstToken = totalTtft,
						InferenceTime = totalInferenceTime,
						ExecutionTime = executionTimer.Elapsed
					};

					_usageStatsCollector.RecordUsage(model.Descriptor.FullName,
						inputTokens, outputTokens,
						cacheHitTokens, cacheMissTokens,
						messageExecutionTimer.ElapsedMilliseconds,
						success: true);

					nativeMessages.AddRange(await Task.WhenAll(toolCallTasks));

					if (agentMessage.ToolCalls.Count == 0 || task.LaunchParameters.Behaviour
						is AgentTaskExecutionBehaviour.ExecuteOnce or AgentTaskExecutionBehaviour.OnlyResponse)
						break;
				}
				catch (Exception ex)
				{
					_usageStatsCollector.RecordUsage(model.Descriptor.FullName,
						0, 0,
						0, 0,
						messageExecutionTimer.ElapsedMilliseconds,
						success: false,
						ex.Message);
					throw;
				}
				finally
				{
					messageExecutionTimer.Stop();
					inferenceTimer.Stop();
				}
			}

			executionTimer.Stop();
		}

		private async Task<IToolMessage> ExecuteToolAsync(IToolCall toolCall, AgentAssistantMessage message,
			AgentTask task, ImmutableDictionary<string, AgentTool> tools, CancellationToken cancellationToken = default)
		{
			// Do not execute tool if the task is configured to only respond.
			if (task.LaunchParameters.Behaviour is AgentTaskExecutionBehaviour.OnlyResponse)
				return new ToolMessage(string.Empty, toolCall.Id, toolCall.ToolName);

			if (toolCall is not IFunctionToolCall functionCall)
				throw new ArgumentException($"Tool call '{toolCall}' is not a function tool call.");

			var agentToolCall = new AgentToolCall
			{
				Status = AgentToolCallStatus.Pending,
				ToolCallId = toolCall.Id,
				ToolName = toolCall.ToolName,
				Arguments = functionCall.Args,
				Result = null
			};
			message.ToolCalls.Add(agentToolCall);

			if (functionCall is PartialFunctionToolCall partialFunctionCall)
			{
				void ArgsPartAdded(object? sender, string e)
				{
					agentToolCall.Arguments = functionCall.Args;
				}
				partialFunctionCall.ArgsPartAdded += ArgsPartAdded;

				try
				{
					await partialFunctionCall;
				}
				finally
				{
					partialFunctionCall.ArgsPartAdded -= ArgsPartAdded;
				}
			}

			if (!tools.TryGetValue(toolCall.ToolName, out var tool))
			{
				agentToolCall.Result = new AgentToolCallResult
				{
					Success = false,
					Content = $"Tool '{toolCall.ToolName}' not found."
				};
				agentToolCall.Status = AgentToolCallStatus.Failed;
				return new ToolMessage(new ToolResult(ToolResultStatus.Error, agentToolCall.Result.Content),
					toolCall.Id, toolCall.ToolName);
			}

			JsonNode? args;
			try
			{
				args = TolerantJsonParser.Parse(functionCall.Args);
			}
			catch (Exception ex)
			{
				agentToolCall.Result = new AgentToolCallResult
				{
					Success = false,
					Content = $"Failed to parse args: " + ex.Message // Even with tolerant parser lol
				};
				agentToolCall.Status = AgentToolCallStatus.Failed;
				return new ToolMessage(new ToolResult(ToolResultStatus.Error, agentToolCall.Result.Content),
					toolCall.Id, toolCall.ToolName);
			}

			try
			{
				agentToolCall.Status = AgentToolCallStatus.PreExecuting;
				var previewResult = await tool.PreExecuteAsync(args, cancellationToken);
				agentToolCall.ExpectedBehaviour = previewResult.ExpectedBehaviour;

				if (previewResult.InterruptingSuccess != null)
				{
					agentToolCall.Result = new AgentToolCallResult
					{
						Success = previewResult.InterruptingSuccess.Value,
						Content = string.IsNullOrEmpty(previewResult.InterruptingContent) ?
							(previewResult.InterruptingSuccess.Value ? "Tool execution completed." : "Tool execution failed.")
							: previewResult.InterruptingContent,
						Attachments = previewResult.InterruptingAttachments
					};
					agentToolCall.Status = previewResult.InterruptingSuccess.Value ? AgentToolCallStatus.Success : AgentToolCallStatus.Failed;
					return new ToolMessage(new ToolResult(previewResult.InterruptingSuccess.Value ? ToolResultStatus.Success : ToolResultStatus.Error,
						agentToolCall.Result.Content, agentToolCall.Result.Attachments.Select(ConvertAttachmentFromAgent)), toolCall.Id, toolCall.ToolName);
				}

				ToolBehaviour autoApproveBehaviours = task.LaunchParameters.AutoApproveBehaviours,
					disallowedBehaviours = task.LaunchParameters.DisallowedBehaviours;

				var (decision, decisionMessage) = _toolApprovalService.ApproveTool(task.LaunchParameters.TriggeredChat,
					tool.ApprovalLevel, previewResult.ExpectedBehaviour, autoApproveBehaviours, disallowedBehaviours);

				if (decision == ToolPolicyDecision.Disallow)
				{
					agentToolCall.Result = new AgentToolCallResult
					{
						Success = false,
						Content = decisionMessage
					};
					agentToolCall.Status = AgentToolCallStatus.Failed;
					return new ToolMessage(new ToolResult(ToolResultStatus.Error, agentToolCall.Result.Content),
						toolCall.Id, toolCall.ToolName);
				}

				string? additionalNotes = null;

				if (decision == ToolPolicyDecision.Ask)
				{
					var request = new AgentToolCallConfirmationRequest
					{
						ToolCall = agentToolCall,
						UserConfirmationSource = new TaskCompletionSource<ToolConsentResult>()
					};

					task.ToolCallConfirmationRequests.Add(request);
					try
					{
						agentToolCall.Status = AgentToolCallStatus.Confirming;
						var consentResult = await request.UserConfirmationSource.Task.WaitAsync(cancellationToken);
						_toolApprovalService.MemorizeConsent(consentResult);

						if (!consentResult.IsApproved)
						{
							agentToolCall.Result = new AgentToolCallResult
							{
								Success = false,
								Content = !string.IsNullOrEmpty(consentResult.Notes) ?
									$"User denied the tool execution. Reason: {consentResult.Notes}." :
									"User denied the tool execution without the reason."
							};
							agentToolCall.Status = AgentToolCallStatus.Failed;
							return new ToolMessage(new ToolResult(ToolResultStatus.Error, agentToolCall.Result.Content),
								toolCall.Id, toolCall.ToolName);
						}

						additionalNotes = consentResult.Notes;
					}
					finally
					{
						task.ToolCallConfirmationRequests.Remove(request);
					}
				}

				agentToolCall.Status = AgentToolCallStatus.Executing;
				var result = await tool.ExecuteAsync(args, previewResult.SharedContext, cancellationToken);

				agentToolCall.Result = new AgentToolCallResult
				{
					Success = result.Success,
					Content = string.IsNullOrEmpty(additionalNotes) ?
						$"{result.Content}\nUser has provided additional notes: {additionalNotes}." :
						result.Content,
					Attachments = result.Attachments
				};
				agentToolCall.Status = result.Success ? AgentToolCallStatus.Success : AgentToolCallStatus.Failed;
				return new ToolMessage(new ToolResult(result.Success ? ToolResultStatus.Success : ToolResultStatus.Error,
					agentToolCall.Result.Content, agentToolCall.Result.Attachments.Select(ConvertAttachmentFromAgent)), toolCall.Id, toolCall.ToolName);
			}
			catch (AggregateException aex) when (aex.InnerExceptions.Any(e => e is OperationCanceledException))
			{
				agentToolCall.Result = new AgentToolCallResult
				{
					Success = false,
					Content = $"Tool execution was canceled."
				};
				agentToolCall.Status = AgentToolCallStatus.Cancelled;
				return new ToolMessage(new ToolResult(ToolResultStatus.Error, agentToolCall.Result.Content),
					toolCall.Id, toolCall.ToolName);
			}
			catch (OperationCanceledException)
			{
				agentToolCall.Result = new AgentToolCallResult
				{
					Success = false,
					Content = $"Tool execution was canceled."
				};
				agentToolCall.Status = AgentToolCallStatus.Cancelled;
				return new ToolMessage(new ToolResult(ToolResultStatus.Error, agentToolCall.Result.Content),
					toolCall.Id, toolCall.ToolName);
			}
			catch (Exception ex)
			{
				agentToolCall.Result = new AgentToolCallResult
				{
					Success = false,
					Content = $"Tool execution failed: " + ex.Message
				};
				agentToolCall.Status = AgentToolCallStatus.Failed;
				return new ToolMessage(new ToolResult(ToolResultStatus.Error, agentToolCall.Result.Content),
					toolCall.Id, toolCall.ToolName);
			}
		}

		private static IEnumerable<IMessage> ConvertMessageFromAgent(AgentChatMessage agentMessage)
		{
			switch (agentMessage)
			{
				case AgentSystemMessage systemMessage:
					return [new SystemMessage(systemMessage.Content ?? string.Empty)];

				case AgentUserMessage userMessage:
					return [new UserMessage(Senders.User, userMessage.Content ?? string.Empty,
						userMessage.Attachments.Select(ConvertAttachmentFromAgent))];

				case AgentAssistantMessage assistantMessage:

					var toolCalls = new List<IToolCall>();
					var toolMessages = new List<IMessage>();

					foreach (var toolCall in assistantMessage.ToolCalls)
					{
						toolCalls.Add(new FunctionToolCall(toolCall.ToolCallId, toolCall.ToolName, toolCall.Arguments));
						toolMessages.Add(new ToolMessage(ConvertToolResultFromAgent(toolCall.Result), toolCall.ToolCallId, toolCall.ToolName));
					}

					var nativeAssistantMessage = new AssistantMessage(assistantMessage.Content, assistantMessage.ReasoningContent,
						toolCalls, assistantMessage.Attachments.Select(ConvertAttachmentFromAgent));

					return [nativeAssistantMessage, ..toolMessages];

				default:
					throw new ArgumentOutOfRangeException(nameof(agentMessage), $"Unknown message type: {agentMessage.GetType()}");
			}
		}

		private static ToolResult ConvertToolResultFromAgent(AgentToolCallResult? agentToolResult)
		{
			if (agentToolResult == null)
				return new ToolResult(ToolResultStatus.NoResult, "Tool call has given no result.");

			var status = agentToolResult.Success ? ToolResultStatus.Success : ToolResultStatus.Error;
			return new ToolResult(status, agentToolResult.Content, agentToolResult.Attachments.Select(ConvertAttachmentFromAgent));
		}

		private static IAttachment ConvertAttachmentFromAgent(AgentAttachment agentAttachment)
		{
			switch (agentAttachment.Type)
			{
				case AgentAttachmentType.Image:
					return new ImageAttachment(agentAttachment.Url);
				case AgentAttachmentType.Audio:
					return new AudioAttachment(agentAttachment.Url);
				case AgentAttachmentType.Video:
					return new VideoAttachment(agentAttachment.Url);

				default:
					throw new ArgumentOutOfRangeException(nameof(agentAttachment), $"Unknown attachment type: {agentAttachment.Type}");
			}
		}
	}
}
