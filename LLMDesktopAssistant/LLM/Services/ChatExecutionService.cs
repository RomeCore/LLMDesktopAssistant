using DocumentFormat.OpenXml.VariantTypes;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Tools;
using LLMDesktopAssistant.Tools;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Metadata;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;
using Serilog;
using System.Collections.Concurrent;
using System.Collections.Immutable;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// The default implementation of the <see cref="IChatExecutionService"/>.
	/// </summary>
	public class ChatExecutionService(
		Chat chat,
		IChatStorageService storage,
		IPromptChatBuilder promptBuilder,
		IToolExecutionService toolExecutor,
		IChatSummarizationService summarizer,
		ILLMBuildingService llmBuilder,
		IToolsetBuildingService toolsetBuilder,
		IMCPManagementService mcpManager,
		IUsageStatsCollector usageStatsCollector
	) : IChatExecutionService
	{
		private CancellationTokenSource? _cts = null;

		public async Task GenerateResponseAsync(CancellationToken cancellationToken = default)
		{
			try
			{
				_cts?.Cancel();
				_cts?.Dispose();

				_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				cancellationToken = _cts.Token;

				await mcpManager.EnsureCurrentMCPConnectionsAsync(cancellationToken);

				var llmInfo = llmBuilder.BuildChatLLM();
				if (llmInfo == null)
					throw new InvalidOperationException("Chat LLM is not configured. Please configure it first.");
				var llm = llmInfo.LLM;

				var timeRequested = DateTime.Now;
				DateTime? timeFirstToken = null;

				var inputMessages = promptBuilder.Build();
				var tools = toolsetBuilder.BuildTools().ToImmutableDictionary(t => t.Name);
				var toolset = new ImmutableToolSet(tools.Values.Select(t => t.Tool));
				var response = await llm.ChatStreamingAsync(inputMessages, tools: toolset, cancellationToken: cancellationToken);
				var responseMessage = response.Message;

				var completionSource = new CompletionSource();
				var domainResponseMessage = new Domain.AssistantMessage
				{
					Status = AssistantMessageStatus.Pending,
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
						if (toolCall is not FunctionToolCall funtionCall)
							throw new InvalidOperationException($"Unsupported tool call type: {toolCall.GetType()}.");

						if (!tools.TryGetValue(toolCall.ToolName, out var toolInfo))
							toolInfo = null;

						var toolCallCompletionSource = new CompletionSource();
						var domainToolCall = new Domain.ToolCall
						{
							Status = ToolStatus.None,
							Id = toolCall.Id,
							ToolName = toolCall.ToolName,
							Title = toolInfo?.DisplayName,
							Arguments = funtionCall.Args,
							CompletionToken = toolCallCompletionSource.Token
						};
						domainResponseMessage.ToolCalls.Add(domainToolCall);

						static async Task WrapToolExecutionTask(
							IToolExecutionService toolExecutor,
							ToolCall domainToolCall,
							ILLMBuildingService llmBuilder,
							ImmutableDictionary<string, ToolInfo> tools,
							LLMInfo llmInfo,
							CompletionSource toolCallCompletionSource,
							CancellationToken cancellationToken)
						{
							try
							{
								await toolExecutor.ExecuteAsync(domainToolCall, llmInfo, tools, cancellationToken);
							}
							finally
							{
								toolCallCompletionSource.Complete();
							}
						}

						var toolExecTask = WrapToolExecutionTask(toolExecutor, domainToolCall, llmBuilder,
							tools, llmInfo, toolCallCompletionSource, cancellationToken);
						lock (lockObj)
							toolExecutionTasks.Add(toolExecTask);
					}

					void PartHandler(object? s, AssistantMessageDelta delta)
					{
						timeFirstToken ??= DateTime.Now;
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
							Log.Information("Response generated successfully! Time to first token: {TimeToFirstToken} s, generation time: {GenerationTime} s",
								(timeFirstToken!.Value - timeRequested).TotalSeconds, (timeReponseFinished - timeFirstToken.Value).TotalSeconds);

							prefixReasoningContent = string.Empty;
							prefixContent = string.Empty;

							var usageMetadata = response.UsageMetadata;
							if (usageMetadata != null)
							{
								if (usageMetadata is IUsageCacheMetadata usageCacheMetadata)
								{
									Log.Information("Input cache hit tokens: {InputCacheHitTokens}, Input cache miss tokens: {InputCacheMissTokens}, Output tokens: {OutputTokens}",
										usageCacheMetadata.InputCacheHitTokens, usageCacheMetadata.InputCacheMissTokens, usageCacheMetadata.OutputTokens);

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
									Log.Information("Input tokens: {InputTokens}, Output tokens: {OutputTokens}",
										usageMetadata.InputTokens, usageMetadata.OutputTokens);

									usageStatsCollector.RecordUsage(
										model: llmInfo.LLM.Descriptor.FullName,
										inputTokens: usageMetadata.InputTokens,
										outputTokens: usageMetadata.OutputTokens,
										durationMs: (long)(timeReponseFinished - timeRequested).TotalMilliseconds,
										success: true);
								}

								await summarizer.TrySummarizeChat(llmInfo, usageMetadata);
							}

							domainResponseMessage.Status = cancellationToken.IsCancellationRequested ?
								AssistantMessageStatus.Cancelled : AssistantMessageStatus.Success;
						}
						catch (OperationCanceledException)
						{
							domainResponseMessage.Status = AssistantMessageStatus.Cancelled;
							RecordFailedUsage(llmInfo, timeRequested, "Operation cancelled");
						}
						catch (AggregateException aex) when (aex.InnerExceptions.Any(e => e is OperationCanceledException))
						{
							domainResponseMessage.Status = AssistantMessageStatus.Cancelled;
							RecordFailedUsage(llmInfo, timeRequested, "Operation cancelled");
						}
						catch (Exception ex)
						{
							domainResponseMessage.Error = ex.ToString();
							domainResponseMessage.Status = AssistantMessageStatus.Error;
							RecordFailedUsage(llmInfo, timeRequested, ex.Message);
						}
						finally
						{
							responseMessage.PartAdded -= PartHandler;
							Log.Information("Generation finished with status: {Status}, error: {Error}, tool call count: {ToolCallCount}, model: {Model}",
								domainResponseMessage.Status, domainResponseMessage.Error, domainResponseMessage.ToolCalls.Count, llm.Descriptor.FullName);
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
						}
					}

					if (toolExecutionTasks.Count == 0)
						break;

					timeRequested = DateTime.Now;
					timeFirstToken = null;

					inputMessages = promptBuilder.Build();
					tools = toolsetBuilder.BuildTools().ToImmutableDictionary(t => t.Name);
					toolset = new ImmutableToolSet(tools.Values.Select(t => t.Tool));
					response = await llm.ChatStreamingAsync(inputMessages, tools: toolset, cancellationToken: cancellationToken);
					responseMessage = response.Message;

					completionSource = new CompletionSource();
					domainResponseMessage = new Domain.AssistantMessage
					{
						Status = AssistantMessageStatus.Pending,
						CompletionToken = completionSource.Token
					};
					storage.AppendMessage(domainResponseMessage);
				}
			}
			catch (Exception ex)
			{
				Log.Error(ex, "An error occurred while generating the response: {ErrorMessage}", ex.Message);
			}
		}

		/// <summary>
		/// Records failed usage statistics when an error occurs.
		/// </summary>
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