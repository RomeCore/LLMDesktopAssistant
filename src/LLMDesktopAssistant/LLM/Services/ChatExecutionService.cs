using LLMDesktopAssistant.Controls.Toasts;
using LLMDesktopAssistant.Data;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.MVVM.Additional.Context;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.LLM.Services.Prompting;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Providers;
using LLMDesktopAssistant.Services.Instances;
using Material.Icons;
using RCLargeLanguageModels;
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
		IChatSettingsService chatSettings,
		IAgentOrderingService agentOrderer,
		IAgentManagementService agentManager,
		IChatStorageService storage,
		IChatPromptBuilder promptBuilder,
		IModelManager modelManager,
		IToolExecutionService toolExecutor,
		ILLMPropertiesBuilder propertiesBuilder,
		IEnumerable<IChatExecutionHook> executionHooks,
		IToolsetCacheService toolsetCache,
		IMCPManagementService mcpManager,
		IUsageStatsCollector usageStatsCollector,
		IToastService toastService,
		IChatExecutionStatusService executionStatusService,
		IChatStatusService statusService
	) : IChatExecutionService
	{
		private readonly List<IChatExecutionHook> _executionHooks = executionHooks.OrderBy(h => h.Order).ToList();
		private CancellationTokenSource? _cts = null;

		public async Task GenerateResponseAsync(CancellationToken cancellationToken = default)
		{
			using var execution = executionStatusService.WithExecution();

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
						statusService.Icon = MaterialIconKind.RobotConfused;
						statusService.Text = LocalizationManager.LocalizeStatic("chat.status.selecting_agent");

						var agentTuple = await agentOrderer.GetNextAgentAsync(cancellationToken);
						nextAgentId = agentTuple?.Item1;
						agentStageId = agentTuple?.Item2;
					}

					if (nextAgentId == null || agentStageId == null)
					{
						if (cycles == 0)
							toastService.ShowWarning(LocalizationManager.LocalizeStatic("chat.toast.agent_selection_failed.title"),
								LocalizationManager.LocalizeStatic("chat.toast.agent_selection_failed.description"));
						else
							await RunExecutionFinishedHooksAsync(cancellationToken);
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
				toastService.ShowError(LocalizationManager.LocalizeStatic("chat.toast.generation_failed.title"),
					LocalizationManager.LocalizeStaticFormat("chat.toast.generation_failed.description", ex.Message));
				throw;
			}
			finally
			{
				statusService.Icon = MaterialIconKind.ChatProcessing;
				statusService.Text = null;
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

				var agent = agentManager.GetAgentDescriptor(agentId);

				LLModel llm;
				try
				{
					var customModel = agent.Generation.GetEffectiveCustomModel(chatSettings.Settings);
					var modelName = customModel.EnableCustomModel && !string.IsNullOrEmpty(customModel.Model)
						? customModel.Model
						: chatSettings.Settings.Models.GetEffectiveSelection().ChatModel;
					llm = modelManager.GetModel(modelName);
				}
				catch
				{
					var toastTitle = LocalizationManager.LocalizeStatic("chat.toast.llm_not_configured.title");
					var toastDesc = LocalizationManager.LocalizeStatic("chat.toast.llm_not_configured.description");
					toastService.ShowError(toastTitle, toastDesc);
					throw new ToastedException(toastTitle, toastDesc);
				}
				llm = llm.WithProperties(propertiesBuilder.BuildProperties(agent, chatSettings.Settings));

				if (mcpManager.HasMCPConnections())
				{
					statusService.Icon = MaterialIconKind.Connection;
					statusService.Text = LocalizationManager.LocalizeStatic("chat.status.waiting_for_mcp_connections");

					await mcpManager.EnsureCurrentMCPConnectionsAsync(cancellationToken);
				}

				var completionSource = new CompletionSource();
				var responsesBuilder = ImmutableList.CreateBuilder<Domain.AssistantMessage>();
				var domainResponseMessage = new Domain.AssistantMessage
				{
					CreatedAt = DateTime.Now,
					Status = AssistantMessageStatus.Pending,
					SenderAgentId = agentId,
					AgentStageId = agentStageId,
					CompletionToken = completionSource.Token
				};
				responsesBuilder.Add(domainResponseMessage);
				int cycle = 0;

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

				var timeRequested = DateTime.Now;
				DateTime? timeFirstToken = null;

				await RunResponsePrepareHooksAsync(new ChatPrepareExecutionHookContext
				{
					Chat = chat,
					Agent = agent,
					Response = domainResponseMessage,
					Cycle = cycle
				}, cancellationToken);

				statusService.Icon = MaterialIconKind.ChatProcessing;
				statusService.Text = LocalizationManager.LocalizeStatic("chat.status.waiting_for_first_response");

				var inputMessages = promptBuilder.Build(agent);
				toolsetCache.Invalidate(agent);
				// Lul, provider caching is fixed now!
				var toolset = toolsetCache.ValidTools.Values.Select(t => t.Tool).OrderBy(t => t.Name);
				// Reveal messages that are marked with 'RevealAfterSend' visibility
				var response = await llm.ChatStreamingAsync(inputMessages, tools: toolset, cancellationToken: cancellationToken);
				var responseMessage = response.Message;

				List<Task> toolExecutionTasks = [];
				var lockObj = new object();

				while (true)
				{
					toolExecutionTasks.Clear();

					void ProcessToolCall(IToolCall toolCall)
					{
						if (toolCall is not IFunctionToolCall funtionCall)
							throw new InvalidOperationException($"Unsupported tool call type: {toolCall.GetType()}.");

						if (toolsetCache.ValidAliasedTools.TryGetValue(toolCall.ToolName, out var toolInfo))
						{
							if (toolInfo.Name != toolCall.ToolName)
							{
								Log.Information($"Tool call '{toolCall.ToolName}' is aliased as '{toolInfo.Name}'. Using the alias.");
							}
						}

						var toolCallCompletionSource = new CompletionSource();
						var domainToolCall = new Domain.ToolCall
						{
							Status = ToolStatus.None,
							Id = toolCall.Id,
							ToolName = toolInfo?.Name ?? toolCall.ToolName,
							Title = toolInfo?.TitleKey,
							Arguments = funtionCall.Args,
							CompletionToken = toolCallCompletionSource.Token
						};
						domainResponseMessage.ToolCalls.Add(domainToolCall);

						async Task WrapToolExecutionTask()
						{
							try
							{
								await toolExecutor.ExecuteAsync(funtionCall as PartialFunctionToolCall,
									domainResponseMessage, domainToolCall, toolInfo, cancellationToken);
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

							statusService.Icon = MaterialIconKind.ChatProcessing;
							statusService.Text = null;
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
										ModelName = llm.Descriptor.FullName,
										InputTokens = usageMetadata.InputTokens,
										InputCacheHitTokens = usageCacheMetadata.InputCacheHitTokens,
										InputCacheMissTokens = usageCacheMetadata.InputCacheMissTokens,
										OutputTokens = usageMetadata.OutputTokens,
										TTFT = (timeFirstToken!.Value - timeRequested).TotalSeconds,
										GenerationTime = (timeReponseFinished - timeFirstToken.Value).TotalSeconds,
									});

									usageStatsCollector.RecordUsage(
										model: llm.Descriptor.FullName,
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
										ModelName = llm.Descriptor.FullName,
										InputTokens = usageMetadata.InputTokens,
										InputCacheHitTokens = null,
										InputCacheMissTokens = null,
										OutputTokens = usageMetadata.OutputTokens,
										TTFT = (timeFirstToken!.Value - timeRequested).TotalSeconds,
										GenerationTime = (timeReponseFinished - timeFirstToken.Value).TotalSeconds,
									});

									usageStatsCollector.RecordUsage(
										model: llm.Descriptor.FullName,
										inputTokens: usageMetadata.InputTokens,
										outputTokens: usageMetadata.OutputTokens,
										durationMs: (long)(timeReponseFinished - timeRequested).TotalMilliseconds,
										success: true);
								}

								await RunResponseCompletedHooksAsync(new ChatAgentResponseExecutionHookContext
								{
									Chat = chat,
									Agent = agent,
									Response = domainResponseMessage,
									UsageMetadata = usageMetadata,
									HasToolCalls = toolExecutionTasks.Count > 0,
									Cycle = cycle
								}, cancellationToken);
							}
							else
							{
								domainResponseMessage.AdditionalViewModels.Add(new TokenCostViewModel
								{
									ModelName = llm.Descriptor.FullName,
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
							RecordFailedUsage(llm, timeRequested, "Operation cancelled");
							throw;
						}
						catch (AggregateException aex) when (aex.InnerExceptions.Any(e => e is OperationCanceledException))
						{
							domainResponseMessage.Status = AssistantMessageStatus.Cancelled;
							RecordFailedUsage(llm, timeRequested, "Operation cancelled");
							throw;
						}
						catch (Exception ex)
						{
							domainResponseMessage.Error = ex.ToString();
							domainResponseMessage.Status = AssistantMessageStatus.Error;
							RecordFailedUsage(llm, timeRequested, ex.Message);
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
							// Invoke execution-finished hooks (e.g. auto-naming) fire-and-forget
							if (toolExecutionTasks.Count == 0)
								await RunAgentExecutionFinishedHooksAsync(new ChatAgentExecutionHookContext
								{
									Chat = chat,
									Agent = agent,
									Responses = responsesBuilder.ToImmutable()
								}, cancellationToken);

							completionSource.Complete();
							cancellationToken.ThrowIfCancellationRequested();
						}
					}

					if (toolExecutionTasks.Count == 0)
						break;

					completionSource = new CompletionSource();
					domainResponseMessage = new Domain.AssistantMessage
					{
						CreatedAt = DateTime.Now,
						Status = AssistantMessageStatus.Pending,
						SenderAgentId = agentId,
						AgentStageId = agentStageId,
						CompletionToken = completionSource.Token
					};
					responsesBuilder.Add(domainResponseMessage);
					cycle++;

					storage.AppendMessage(domainResponseMessage);

					timeRequested = DateTime.Now;
					timeFirstToken = null;

					await RunResponsePrepareHooksAsync(new ChatPrepareExecutionHookContext
					{
						Chat = chat,
						Agent = agent,
						Response = domainResponseMessage,
						Cycle = cycle
					}, cancellationToken);

					statusService.Icon = MaterialIconKind.ChatProcessing;
					statusService.Text = LocalizationManager.LocalizeStatic("chat.status.waiting_for_first_response");

					inputMessages = promptBuilder.Build(agent);
					toolsetCache.Invalidate(agent);
					toolset = toolsetCache.ValidTools.Values.Select(t => t.Tool).OrderBy(t => t.Name);
					response = await llm.ChatStreamingAsync(inputMessages, tools: toolset, cancellationToken: cancellationToken);
					responseMessage = response.Message;
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
				statusService.Icon = MaterialIconKind.ChatProcessing;
				statusService.Text = null;
			}
		}

		/// <summary>
		/// Invokes <see cref="IChatExecutionHook.OnResponsePrepareAsync"/> on all registered
		/// hooks in ascending <see cref="IChatExecutionHook.Order"/>, awaiting each one.
		/// Failures are logged and do not propagate to the execution pipeline.
		/// </summary>
		/// <param name="context">The context of the preview response cycle.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		private async Task RunResponsePrepareHooksAsync(ChatPrepareExecutionHookContext context, CancellationToken cancellationToken)
		{
			foreach (var hook in _executionHooks)
			{
				try
				{
					await hook.OnResponsePrepareAsync(context, cancellationToken);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Hook {Hook} failed in OnResponsePrepare: {Error}", hook.GetType().Name, ex.Message);
				}
			}
		}

		/// <summary>
		/// Invokes <see cref="IChatExecutionHook.OnAgentResponseCompletedAsync"/> on all registered
		/// hooks in ascending <see cref="IChatExecutionHook.Order"/>, awaiting each one.
		/// Failures are logged and do not propagate to the execution pipeline.
		/// </summary>
		/// <param name="context">The context of the completed response cycle.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		private async Task RunResponseCompletedHooksAsync(ChatAgentResponseExecutionHookContext context, CancellationToken cancellationToken)
		{
			foreach (var hook in _executionHooks)
			{
				try
				{
					await hook.OnAgentResponseCompletedAsync(context, cancellationToken);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Hook {Hook} failed in OnResponseCompleted: {Error}", hook.GetType().Name, ex.Message);
				}
			}
		}

		/// <summary>
		/// Invokes <see cref="IChatExecutionHook.OnAgentExecutionFinishedAsync"/> on all registered
		/// hooks in ascending <see cref="IChatExecutionHook.Order"/> without awaiting them
		/// (fire-and-forget). Failures are logged and do not propagate to the execution pipeline.
		/// </summary>
		/// <param name="context">The context of the finished execution.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		private Task RunAgentExecutionFinishedHooksAsync(ChatAgentExecutionHookContext context, CancellationToken cancellationToken)
		{
			foreach (var hook in _executionHooks)
			{
				try
				{
					_ = hook.OnAgentExecutionFinishedAsync(context, cancellationToken);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Hook {Hook} failed in OnExecutionFinished: {Error}", hook.GetType().Name, ex.Message);
				}
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Invokes <see cref="IChatExecutionHook.OnExecutionFinishedAsync"/> on all registered
		/// hooks in ascending <see cref="IChatExecutionHook.Order"/> without awaiting them
		/// (fire-and-forget). Failures are logged and do not propagate to the execution pipeline.
		/// </summary>
		/// <param name="context">The context of the finished execution.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		private Task RunExecutionFinishedHooksAsync(CancellationToken cancellationToken)
		{
			foreach (var hook in _executionHooks)
			{
				try
				{
					_ = hook.OnExecutionFinishedAsync(chat, cancellationToken);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception ex)
				{
					Log.Error(ex, "Hook {Hook} failed in OnExecutionFinished: {Error}", hook.GetType().Name, ex.Message);
				}
			}

			return Task.CompletedTask;
		}

		private void RecordFailedUsage(LLModel llm, DateTime timeRequested, string errorMessage)
		{
			try
			{
				usageStatsCollector.RecordUsage(
					model: llm.Descriptor.FullName,
					inputTokens: 0,
					outputTokens: 0,
					durationMs: (long)(DateTime.Now - timeRequested).TotalMilliseconds,
					success: false,
					errorMessage: errorMessage);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to record usage statistics for failed request");
			}
		}
	}
}