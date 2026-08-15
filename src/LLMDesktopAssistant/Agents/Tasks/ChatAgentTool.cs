using System.Text.Json.Nodes;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Tools;
using RCLargeLanguageModels.Tasks;
using RCLargeLanguageModels.Tools;

namespace LLMDesktopAssistant.Agents.Tasks
{
	public class ChatAgentTool : AgentTool
	{
		/// <summary>
		/// The tool information for the chat tool.
		/// </summary>
		public required ToolInfo ChatToolInfo { get; init; }

		/// <summary>
		/// The execution context for the tool. If null, the default (dummy) context will be created.
		/// </summary>
		public ToolExecutionContext? ExecutionContext { get; init; }

		public override string Name => ChatToolInfo.Name;

		public override string DisplayName => ChatToolInfo.DisplayName ?? ChatToolInfo.Name;

		public override string Description => ChatToolInfo.DescriptionGetter();

		public override JsonObject ArgumentSchema => ChatToolInfo.ArgumentSchema;

		private ToolExecutionContext CreateContext()
		{
			if (ExecutionContext != null)
			{
				return new ToolExecutionContext
				{
					Chat = ExecutionContext.Chat,
					Message = ExecutionContext.Message,
					Call = ExecutionContext.Call,
					Info = ChatToolInfo,
					PolicyDecision = ToolPolicyDecision.None,
					RunningInUI = false,
					SharedContext = null
				};
			}

			var dummyChat = new Chat(ServiceRegistry.Provider);
			var dummyMessage = new AssistantMessage
			{
				CreatedAt = DateTime.Now,
				AgentStageId = Guid.Empty,
				SenderAgentId = Guid.Empty,
				CompletionToken = CompletionToken.Success
			};
			dummyChat.Messages.Add(dummyMessage);
			var dummyToolCall = new ToolCall
			{
				CompletionToken = CompletionToken.Success,
				Id = ToolCallId.Generate(),
				ToolName = ChatToolInfo.Name,
				Title = ChatToolInfo.DisplayName
			};
			dummyMessage.ToolCalls.Add(dummyToolCall);

			return new ToolExecutionContext
			{
				Chat = dummyChat,
				Message = dummyMessage,
				Call = dummyToolCall,
				Info = ChatToolInfo,
				PolicyDecision = ToolPolicyDecision.None,
				RunningInUI = false,
				SharedContext = null
			};
		}

		private static AgentAttachment? TryConvertAttachment(Attachment? attachment)
		{
			return AgentAttachment.TryConvertFromNativeAttachment(attachment?.NativeAttachment);
		}

		public override async Task<AgentToolCallPreResult> PreExecuteAsync(JsonNode? arguments, CancellationToken cancellationToken = default)
		{
			if (ChatToolInfo.PreviewExecutor == null)
			{
				return new AgentToolCallPreResult
				{
					ExpectedBehaviour = ChatToolInfo.DefaultExpectedBehaviour
				};
			}

			var ctx = CreateContext();
			var result = await ChatToolInfo.PreviewExecutor.Invoke(arguments, ctx, cancellationToken);

			return new AgentToolCallPreResult
			{
				InterruptingSuccess = result.InterruptingSuccess,
				InterruptingContent = result.InterruptingContent,
				InterruptingAttachments = result.InterruptingAttachments.Select(TryConvertAttachment).Where(a => a != null).ToImmutableList()!,
				ExpectedBehaviour = result.ExpectedBehaviour ?? ChatToolInfo.DefaultExpectedBehaviour,
				SharedContext = ctx
			};
		}

		public override async Task<AgentToolCallResult> ExecuteAsync(JsonNode? arguments, object? sharedContext, CancellationToken cancellationToken = default)
		{
			var ctx = sharedContext as ToolExecutionContext ?? CreateContext();
			var result = await ChatToolInfo.Executor.Invoke(arguments, ctx, cancellationToken);
			var success = await result.Completion;

			return new AgentToolCallResult
			{
				Success = success,
				Content = result.ResultContent,
				Attachments = result.Attachments.Select(TryConvertAttachment).Where(a => a != null).ToImmutableList()!
			};
		}
	}
}
