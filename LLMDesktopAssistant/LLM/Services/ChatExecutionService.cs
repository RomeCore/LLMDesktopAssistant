using System.Collections.Concurrent;
using LLMDesktopAssistant.LLM.Domain;
using RCLargeLanguageModels.Messages;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;
using Serilog;

namespace LLMDesktopAssistant.LLM.Services
{
	/// <summary>
	/// The default implementation of the <see cref="IChatExecutionService"/>.
	/// </summary>
	public class ChatExecutionService(
		IChatStorageService storage,
		IPromptChatBuilder promptBuilder,
		IToolExecutionService toolExecutor,
		ILLMProvider llmProvider)

		: IChatExecutionService
	{
		private CancellationTokenSource? _cts = null;

		public async Task GenerateResponseAsync(CancellationToken cancellationToken = default)
		{
			_cts?.Cancel();
			_cts?.Dispose();

			_cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			cancellationToken = _cts.Token;

			var llmInfo = llmProvider.GetChatLLM();
			var llm = llmInfo.LLM;
			var toolset = new ImmutableToolSet(llmInfo.Tools.Values.Select(t => t.Tool));

			var inputMessages = promptBuilder.Build();
			var response = await llm.ChatStreamingAsync(inputMessages, tools: toolset, cancellationToken: cancellationToken);
			var responseMessage = response.Message;

			var completionSource = new CompletionSource();
			var domainResponseMessage = new Domain.AssistantMessage
			{
				Status = AssistantMessageStatus.Pending,
				CompletionToken = completionSource.Token
			};
			storage.AppendMessage(domainResponseMessage);

			List<Task> toolExecutionTasks = [];
			var lockObj = new object();

			while (true)
			{
				toolExecutionTasks.Clear();

				void ProcessToolCall(IToolCall toolCall)
				{
					if (toolCall is not FunctionToolCall funtionCall)
						throw new InvalidOperationException($"Unsupported tool call type: {toolCall.GetType()}.");

					var toolCallCompletionSource = new CompletionSource();
					var domainToolCall = new Domain.ToolCall
					{
						Status = ToolStatus.NotExecuted,
						Id = toolCall.Id,
						ToolName = toolCall.ToolName,
						Arguments = funtionCall.Args,
						CompletionToken = toolCallCompletionSource.Token
					};
					domainResponseMessage.ToolCalls.Add(domainToolCall);

					var toolExecTask = toolExecutor.ExecuteAsync(domainToolCall, llmInfo, cancellationToken)
						.ContinueWith(t => toolCallCompletionSource.Complete(), cancellationToken: cancellationToken);
					lock (lockObj)
						toolExecutionTasks.Add(toolExecTask);
				}

				void PartHandler(object? s, AssistantMessageDelta delta)
				{
					domainResponseMessage.Status = AssistantMessageStatus.Streaming;
					if (!string.IsNullOrEmpty(delta.DeltaContent))
						domainResponseMessage.Content = responseMessage.Content;
					if (!string.IsNullOrEmpty(delta.DeltaReasoningContent))
						domainResponseMessage.ReasoningContent = responseMessage.ReasoningContent;
					foreach (var toolCall in delta.NewToolCalls ?? [])
						ProcessToolCall(toolCall);
				}

				domainResponseMessage.Content = responseMessage.Content;
				domainResponseMessage.ReasoningContent = responseMessage.ReasoningContent;
				foreach (var toolCall in responseMessage.ToolCalls)
					ProcessToolCall(toolCall);

				responseMessage.PartAdded += PartHandler;
				try
				{
					try
					{
						await responseMessage;
						domainResponseMessage.Status = AssistantMessageStatus.Success;
					}
					catch (OperationCanceledException)
					{
						domainResponseMessage.Status = AssistantMessageStatus.Cancelled;
					}
					catch (AggregateException aex) when (aex.InnerExceptions.Any(e => e is OperationCanceledException))
					{
						domainResponseMessage.Status = AssistantMessageStatus.Cancelled;
					}
					catch (Exception ex)
					{
						domainResponseMessage.Error = ex.ToString();
						domainResponseMessage.Status = AssistantMessageStatus.Error;
					}
					finally
					{
						responseMessage.PartAdded -= PartHandler;
					}
				}
				finally
				{
					await Task.WhenAll(toolExecutionTasks);
					completionSource.Complete();
					Log.Information("Message finished with status: {Status}, error: {Error}, tool call count: {ToolCallCount}",
						domainResponseMessage.Status, domainResponseMessage.Error, domainResponseMessage.ToolCalls.Count);
				}

				if (toolExecutionTasks.Count == 0)
					break;

				inputMessages = promptBuilder.Build();
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
	}
}