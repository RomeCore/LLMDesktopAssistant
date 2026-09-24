using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.Settings;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Services.Prompting;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Prompting.State;
using RCLargeLanguageModels.Tasks;

namespace LLMDesktopAssistant.Tests.Prompting;

internal sealed class EmptyServiceProvider : IServiceProvider
{
	public object? GetService(Type serviceType) => null;
}

/// <summary>
/// A <see cref="IMessageVisibilityService"/> fake with configurable predicates (everything visible by default).
/// </summary>
internal sealed class FakeMessageVisibilityService : IMessageVisibilityService
{
	public Func<BranchedMessage, bool> IsUserVisible { get; set; } = _ => true;
	public Func<BranchedMessage, bool> IsAssistantVisible { get; set; } = _ => true;

	public bool IsUserMessageVisibleToAgent(BranchedMessage message, ChatAgentDescriptor agent) => IsUserVisible(message);

	public bool IsAssistantMessageVisibleToAgent(BranchedMessage message, ChatAgentDescriptor agent) => IsAssistantVisible(message);
}

internal sealed class FakeSectionState : PromptSectionStateBase
{
}

internal sealed class FakeSectionDelta : PromptSectionDeltaBase
{
}

/// <summary>
/// An <see cref="IPromptSection"/> fake rendering a fixed text and tools.
/// </summary>
internal sealed class FakeSection(int order, string text, params SerializableToolDefinition[] tools) : IPromptSection
{
	public Type StateType => typeof(FakeSectionState);

	public Type DeltaType => typeof(FakeSectionDelta);

	public int Order => order;

	public ChatAgentDescriptor? LastCaptureAgent { get; private set; }

	public PromptSectionStateBase CaptureState(ChatAgentDescriptor agent)
	{
		LastCaptureAgent = agent;
		return new FakeSectionState();
	}

	public SystemPromptSnapshot RenderState(PromptSectionStateBase state) => new()
	{
		Text = text,
		Tools = [..tools]
	};

	public PromptSectionDeltaBase? CalculateDelta(PromptSectionStateBase? anchorState,
		IEnumerable<PromptSectionDeltaBase> existingDeltas, PromptSectionStateBase? actualState,
			EffectiveChatContext context) => null;

	public string RenderDelta(PromptSectionDeltaBase delta) => string.Empty;
}

internal static class PromptingTestHelpers
{
	public static BranchedMessage User(string content, int index) => new()
	{
		Message = new UserMessage
		{
			Content = content,
			CreatedAt = DateTime.UtcNow,
			SenderLogin = "user",
			Visibility = MessageVisibility.Always,
			VisibleTo = [],
			IsVisibleToWhiteList = false
		},
		MessageId = index,
		MessageIndex = index
	};

	public static BranchedMessage Assistant(string content, int index, Guid? senderAgentId = null) => new()
	{
		Message = new AssistantMessage
		{
			Content = content,
			CreatedAt = DateTime.UtcNow,
			SenderAgentId = senderAgentId ?? Guid.NewGuid(),
			AgentStageId = Guid.NewGuid(),
			CompletionToken = new CompletionSource().Token
		},
		MessageId = index,
		MessageIndex = index
	};

	public static Chat CreateChat(params BranchedMessage[] messages)
	{
		var chat = new Chat(new EmptyServiceProvider());
		foreach (var message in messages)
			chat.Messages.Add(message);
		return chat;
	}

	/// <summary>
	/// Creates an agent with all context settings inheriting from the agent level (own values),
	/// so that effective getters resolve deterministically in tests.
	/// </summary>
	public static ChatAgentDescriptor CreateAgent()
	{
		var agent = new ChatAgentDescriptor();
		agent.Context.PromptMode = PromptContextMode.Dynamic;
		agent.Context.MaxVisibleRoundsInheritance = ChatSettingsInheritanceLevel.Agent;
		agent.Context.DisabledFlagsInheritance = ChatSettingsInheritanceLevel.Agent;
		return agent;
	}

	public static ContextCheckpoint AddCheckpoint(BranchedMessage message, ContextCheckpointKind kind, bool enabled = true)
	{
		var checkpoint = new ContextCheckpoint
		{
			Kind = kind,
			IsCompletedAndEnabled = enabled
		};
		message.Message.AdditionalData.TryReplace(checkpoint);
		return checkpoint;
	}

	public static SerializableToolDefinition Tool(string name) => new()
	{
		Name = name,
		Description = name,
		ArgumentSchema = "{}"
	};
}
