using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Net.NetworkInformation;
using DocumentFormat.OpenXml.VariantTypes;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Controls.Toasts;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM.Additional;
using LLMDesktopAssistant.LLM.MVVM.Additional.Context;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services.Instances;
using LLMDesktopAssistant.Tools;
using Material.Icons;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;
using Serilog;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// The default implementation of the <see cref="IChatExecutionService"/>.
	/// </summary>
	[ChatService(typeof(IChatExecutionService))]
	public class ChatExecutionService(
		Chat chat,
		IAgentOrderingService agentOrderer,
		IAgentManagementService agentManager,
		IChatStorageService storage,
		IPromptChatBuilder promptBuilder,
		IToolExecutionService toolExecutor,
		ILLMPropertiesBuilder propertiesBuilder,
		IChatSummarizationService summarizer,
		IChatNamingService namingService,
		ILLMBuildingService llmBuilder,
		IToolsetCacheService toolsetCache,
		IMCPManagementService mcpManager,
		IUsageStatsCollector usageStatsCollector,
		IToastService toastService
	) : IChatExecutionService
	{
		private CancellationTokenSource? _cts = null;

		public async Task GenerateResponseAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				int cycles = 0;

				while (true)
				{
					cancellationToken.ThrowIfCancellationRequested();

					var lastAssistantMessage = chat.Messages.LastOrDefault()?.Message as Domain.AssistantMessage;
					Guid? nextAgentId = lastAssistantMessage != null && lastAssistantMessage.ToolCalls.Count != 0
						? lastAssistantMessage.SenderAgentId
						: null;
					Guid? agentStageId = lastAssistantMessage != null && lastAssistantMessage.ToolCalls.Count != 0
						? lastAssistantMessage.AgentStageId
						: null;

					if (nextAgentId == null || agentStageId == null)
					{
						chat.StatusIcon = MaterialIconKind.RobotConfused;
						chat.StatusText = LocalizationManager.LocalizeStatic("chat_status_selecting_agent");

						var agentTuple = await agentOrderer.GetNextAgentAsync(cancellationToken);
						nextAgentId = agentTuple?.Item1;
						agentStageId = agentTuple?.Item2;
					}

					if (nextAgentId == null || agentStageId == null)
					{
						if (cycles == 0)
							toastService.ShowWarning(LocalizationManager.LocalizeStatic("chat_toast_agent_selection_failed"),
								LocalizationManager.LocalizeStatic("chat_toast_agent_selection_failed_desc"));
						return;
					}

					cancellationToken.ThrowIfCancellationRequested();
					await GenerateResponseWithAgentAsync(nextAgentId.Value, agentStageId.Value, cancellationToken);
					cycles++;
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (ToastedException tex)
			{
				Log.Error(tex, "An error occurred while generating the response using default agent: {ErrorMessage}", tex.Message);
				throw;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "An error occurred while generating the response using default agent: {ErrorMessage}", ex.Message);
				toastService.ShowError(LocalizationManager.LocalizeStatic("chat_toast_generation_failed"),
					LocalizationManager.LocalizeStaticFormat("chat_toast_generation_failed_desc", ex.Message));
				throw;
			}
			finally
			{
				chat.StatusIcon = MaterialIconKind.ChatProcessing;
				chat.StatusText = null;
			}
		}

		public async Task GenerateResponseWithAgentAsync(Guid agentId, Guid agentStageId,
			CancellationToken cancellationToken = default)
		{
			try
			{
				_cts?.Cancel();
				_cts?.Dispose();

				_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				cancellationToken = _cts.Token;

				var llmInfo = llmBuilder.BuildChatLLM(agentId);
				if (llmInfo == null)
				{
					var toastTitle = LocalizationManager.LocalizeStatic("chat_toast_llm_not_configured");
					var toastDesc = LocalizationManager.LocalizeStatic("chat_toast_llm_not_configured_desc");
					toastService.ShowError(toastTitle, toastDesc);
					throw new ToastedException(toastTitle, toastDesc);
				}
				var llm = llmInfo.LLM;
				llm = llm.WithProperties(propertiesBuilder.BuildProperties(agentId));

				if (mcpManager.HasMCPConnections())
				{
					chat.StatusIcon = MaterialIconKind.Connection;
					chat.StatusText = LocalizationManager.LocalizeStatic("chat_status_waiting_for_mcp_connections");

					await mcpManager.EnsureCurrentMCPConnectionsAsync(cancellationToken);
				}

				var timeRequested = DateTime.Now;
				DateTime? timeFirstToken = null;

				chat.StatusIcon = MaterialIconKind.ChatProcessing;
				chat.StatusText = LocalizationManager.LocalizeStatic("chat_status_waiting_for_first_response");

				var agent = agentManager.GetAgentDescriptor(agentId);
				var inputMessages = promptBuilder.Build(agent);
				toolsetCache.Invalidate(agentId);
				var tools = toolsetCache.ValidTools;
				var toolset = new ImmutableToolSet(tools.Values.Select(t => t.Tool));
				var response = await llm.ChatStreamingAsync(inputMessages, tools: toolset, cancellationToken: cancellationToken);
				var responseMessage = response.Message;

				var completionSource = new CompletionSource();
				var domainResponseMessage = new Domain.AssistantMessage
				{
					Status = AssistantMessageStatus.Pending,
					SenderAgentId = agentId,
					AgentStageId = agentStageId,
					CompletionToken = completionSource.Token
				};

				string prefixReasoningContent = string.Empty;
				string prefixContent = string.Empty;

				if (chat.Messages[^1].Message is Domain.AssistantMessage lastAssistantMessage
					&& lastAssistantMessage.ToolCalls.Count == 0 && false)
				{
					prefixReasoningContent = lastAssistantMessage.ReasoningContent ?? string.Empty;
					prefixContent = lastAssistantMessage.Content ?? string.Empty;

					storage.EditMessage(chat.Messages[^1].MessageIndex, domainResponseMessage);
				}
				else
				{
					storage.AppendMessage(domainResponseMessage);
				}

				List<Task> toolExecutionTasks = [];
				var lockObj = new object();

				while (true)
				{
					toolExecutionTasks.Clear();

					void ProcessToolCall(IToolCall toolCall)
					{
						if (toolCall is not IFunctionToolCall funtionCall)
							throw new InvalidOperationException($"Unsupported tool call type: {toolCall.GetType()}.");

						if (!tools.TryGetValue(toolCall.ToolName, out var toolInfo))
							toolInfo = null;

						var toolCallCompletionSource = new CompletionSource();
						var domainToolCall = new Domain.ToolCall
						{
							Status = ToolStatus.None,
							Id = toolCall.Id,
							ToolName = toolCall.ToolName,
							Title = toolInfo?.DisplayName ?? toolCall.ToolName,
							Arguments = funtionCall.Args,
							CompletionToken = toolCallCompletionSource.Token
						};
						domainResponseMessage.ToolCalls.Add(domainToolCall);

						async Task WrapToolExecutionTask()
						{
							if (funtionCall is PartialFunctionToolCall partialFunctionCall)
							{
								domainToolCall.Status = ToolStatus.Pending;
								void AddedPartialArg(object? sender, string deltaArg)
								{
									domainToolCall.Arguments = funtionCall.Args;
								}

								partialFunctionCall.ArgsPartAdded += AddedPartialArg;
								try
								{
									await partialFunctionCall;
								}
								finally
								{
									partialFunctionCall.ArgsPartAdded -= AddedPartialArg;
								}
							}

							try
							{
								await toolExecutor.ExecuteAsync(domainResponseMessage, domainToolCall, llmInfo, tools, cancellationToken);
							}
							finally
							{
								toolCallCompletionSource.Complete();
							}
						}

						var toolExecTask = WrapToolExecutionTask();
						lock (lockObj)
							toolExecutionTasks.Add(toolExecTask);
					}

					void PartHandler(object? s, AssistantMessageDelta delta)
					{
						if (timeFirstToken == null)
						{
							timeFirstToken ??= DateTime.Now;

							chat.StatusIcon = MaterialIconKind.ChatProcessing;
							chat.StatusText = null;
						}

						domainResponseMessage.Status = AssistantMessageStatus.Streaming;

						if (!string.IsNullOrEmpty(delta.DeltaReasoningContent))
							domainResponseMessage.ReasoningContent = prefixReasoningContent + responseMessage.ReasoningContent;
						if (!string.IsNullOrEmpty(delta.DeltaContent))
							domainResponseMessage.Content = prefixContent + responseMessage.Content;

						foreach (var toolCall in delta.NewToolCalls ?? [])
							ProcessToolCall(toolCall);
					}

					domainResponseMessage.ReasoningContent = prefixReasoningContent + responseMessage.ReasoningContent;
					domainResponseMessage.Content = prefixContent + responseMessage.Content;
					foreach (var toolCall in responseMessage.ToolCalls)
						ProcessToolCall(toolCall);

					responseMessage.PartAdded += PartHandler;
					try
					{
						try
						{
							await response;

							timeFirstToken ??= DateTime.Now;
							var timeReponseFinished = DateTime.Now;

							prefixReasoningContent = string.Empty;
							prefixContent = string.Empty;

							var usageMetadata = response.UsageMetadata;
							if (usageMetadata != null)
							{
								if (usageMetadata is IUsageCacheMetadata usageCacheMetadata)
								{
									domainResponseMessage.AdditionalViewModels.Add(new TokenCostViewModel
									{
										ModelName = llmInfo.LLM.Descriptor.FullName,
										InputTokens = usageMetadata.InputTokens,
										InputCacheHitTokens = usageCacheMetadata.InputCacheHitTokens,
										InputCacheMissTokens = usageCacheMetadata.InputCacheMissTokens,
										OutputTokens = usageMetadata.OutputTokens,
										TTFT = (timeFirstToken!.Value - timeRequested).TotalSeconds,
										GenerationTime = (timeReponseFinished - timeFirstToken.Value).TotalSeconds,
									});

									usageStatsCollector.RecordUsage(
										model: llmInfo.LLM.Descriptor.FullName,
										inputTokens: usageMetadata.InputTokens,
										outputTokens: usageMetadata.OutputTokens,
										cacheHitTokens: usageCacheMetadata.InputCacheHitTokens,
										cacheMissTokens: usageCacheMetadata.InputCacheMissTokens,
										durationMs: (long)(timeReponseFinished - timeRequested).TotalMilliseconds,
										success: true);
								}
								else
								{
									domainResponseMessage.AdditionalViewModels.Add(new TokenCostViewModel
									{
										ModelName = llmInfo.LLM.Descriptor.FullName,
										InputTokens = usageMetadata.InputTokens,
										InputCacheHitTokens = null,
										InputCacheMissTokens = null,
										OutputTokens = usageMetadata.OutputTokens,
										TTFT = (timeFirstToken!.Value - timeRequested).TotalSeconds,
										GenerationTime = (timeReponseFinished - timeFirstToken.Value).TotalSeconds,
									});

									usageStatsCollector.RecordUsage(
										model: llmInfo.LLM.Descriptor.FullName,
										inputTokens: usageMetadata.InputTokens,
										outputTokens: usageMetadata.OutputTokens,
										durationMs: (long)(timeReponseFinished - timeRequested).TotalMilliseconds,
										success: true);
								}

								await summarizer.TrySummarizeChatAsync(usageMetadata, cancellationToken);

							// Auto-name the chat if it still has a default title
							_ = namingService.TryNameChatAsync(cancellationToken);
							}
							else
							{
								domainResponseMessage.AdditionalViewModels.Add(new TokenCostViewModel
								{
									ModelName = llmInfo.LLM.Descriptor.FullName,
									InputTokens = null,
									InputCacheHitTokens = null,
									InputCacheMissTokens = null,
									OutputTokens = null,
									TTFT = (timeFirstToken!.Value - timeRequested).TotalSeconds,
									GenerationTime = (timeReponseFinished - timeFirstToken.Value).TotalSeconds,
								});
							}

							domainResponseMessage.Status = cancellationToken.IsCancellationRequested ?
								AssistantMessageStatus.Cancelled : AssistantMessageStatus.Success;
						}
						catch (OperationCanceledException)
						{
							domainResponseMessage.Status = AssistantMessageStatus.Cancelled;
							RecordFailedUsage(llmInfo, timeRequested, "Operation cancelled");
							throw;
						}
						catch (AggregateException aex) when (aex.InnerExceptions.Any(e => e is OperationCanceledException))
						{
							domainResponseMessage.Status = AssistantMessageStatus.Cancelled;
							RecordFailedUsage(llmInfo, timeRequested, "Operation cancelled");
							throw;
						}
						catch (Exception ex)
						{
							domainResponseMessage.Error = ex.ToString();
							domainResponseMessage.Status = AssistantMessageStatus.Error;
							RecordFailedUsage(llmInfo, timeRequested, ex.Message);
							throw;
						}
						finally
						{
							responseMessage.PartAdded -= PartHandler;
						}
					}
					finally
					{
						try
						{
							await Task.WhenAll(toolExecutionTasks);
						}
						catch
						{
						}
						finally
						{
							completionSource.Complete();
							cancellationToken.ThrowIfCancellationRequested();
						}
					}

					if (toolExecutionTasks.Count == 0)
						break;

					timeRequested = DateTime.Now;
					timeFirstToken = null;

					chat.StatusIcon = MaterialIconKind.ChatProcessing;
					chat.StatusText = LocalizationManager.LocalizeStatic("chat_status_waiting_for_first_response");

					inputMessages = promptBuilder.Build(agent);
					toolsetCache.Invalidate(agentId);
					tools = toolsetCache.ValidTools;
					toolset = new ImmutableToolSet(tools.Values.Select(t => t.Tool));
					response = await llm.ChatStreamingAsync(inputMessages, tools: toolset, cancellationToken: cancellationToken);
					responseMessage = response.Message;

					completionSource = new CompletionSource();
					domainResponseMessage = new Domain.AssistantMessage
					{
						Status = AssistantMessageStatus.Pending,
						SenderAgentId = agentId,
						AgentStageId = agentStageId,
						CompletionToken = completionSource.Token
					};
					storage.AppendMessage(domainResponseMessage);
				}
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception ex)
			{
				Log.Error(ex, "An error occurred while generating the response using agent: {ErrorMessage}", ex.Message);
				throw;
			}
			finally
			{
				chat.StatusIcon = MaterialIconKind.ChatProcessing;
				chat.StatusText = null;
			}
		}

		private void RecordFailedUsage(LLMInfo? llmInfo, DateTime timeRequested, string errorMessage)
		{
			try
			{
				if (llmInfo != null)
				{
					usageStatsCollector.RecordUsage(
						model: llmInfo.LLM.Descriptor.FullName,
						inputTokens: 0,
						outputTokens: 0,
						durationMs: (long)(DateTime.Now - timeRequested).TotalMilliseconds,
						success: false,
						errorMessage: errorMessage);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to record usage statistics for failed request");
			}
		}
	}
}