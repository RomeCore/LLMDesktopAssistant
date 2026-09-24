using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.Settings;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Prompting;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Prompting.State;
using LLMDesktopAssistant.Tests.Storage;

namespace LLMDesktopAssistant.Tests.Prompting;

/// <summary>
/// Tests for the SCM stage: anchor lifecycle (create/reuse/rebaseline) and agent isolation.
/// </summary>
[Collection("Prompting")]
public class PromptStateStageTests
{
	private static PromptStateProcessor CreateStage(Chat chat, params IPromptSection[] sections)
		=> new(chat, sections);

	private static ChatAgentDescriptor CreateHybridAgent()
	{
		var agent = PromptingTestHelpers.CreateAgent();
		agent.Context.PromptMode = PromptContextMode.Hybrid;
		return agent;
	}

	[Fact]
	public void CreatesAnchor_OnFirstHybridPrep()
	{
		var u0 = PromptingTestHelpers.User("u0", 0);
		var chat = PromptingTestHelpers.CreateChat(u0);
		var section = new FakeSection(0, "core");
		var stage = CreateStage(chat, section);
		var effective = new EffectiveChatContext([u0], [], -1);

		var anchor = stage.Process(CreateHybridAgent(), effective);

		Assert.NotNull(anchor);
		Assert.Equal(1, anchor!.Id);
		Assert.Equal("core", anchor.Snapshot.Text);
		Assert.False(anchor.IsVisible);
		Assert.Single(anchor.Sections);
		var stored = Assert.Single(u0.Message.AdditionalData.GetAll<PromptStateAnchorMessageData>());
		Assert.Same(anchor, stored);
		Assert.True(chat.AdditionalData.TryGet<PromptStateAnchorIdCounter>(out var counter));
		Assert.Equal(1, counter.LastId);
	}

	[Fact]
	public void ReusesAnchor_OnSecondPrep()
	{
		var u0 = PromptingTestHelpers.User("u0", 0);
		var chat = PromptingTestHelpers.CreateChat(u0);
		var stage = CreateStage(chat, new FakeSection(0, "core"));
		var agent = CreateHybridAgent();
		var effective = new EffectiveChatContext([u0], [], -1);

		var first = stage.Process(agent, effective);
		var second = stage.Process(agent, effective);

		Assert.NotNull(first);
		Assert.Same(first, second);
		Assert.Single(u0.Message.AdditionalData.GetAll<PromptStateAnchorMessageData>());
		Assert.True(chat.AdditionalData.TryGet<PromptStateAnchorIdCounter>(out var counter));
		Assert.Equal(1, counter.LastId);
	}

	[Fact]
	public void Rebaselines_WhenAnchorIsBeforeNewCut()
	{
		var u0 = PromptingTestHelpers.User("u0", 0);
		var u1 = PromptingTestHelpers.User("u1", 1);
		var chat = PromptingTestHelpers.CreateChat(u0, u1);
		var stage = CreateStage(chat, new FakeSection(0, "core"));
		var agent = CreateHybridAgent();

		var first = stage.Process(agent, new EffectiveChatContext([u0, u1], [], -1));
		Assert.NotNull(first);
		Assert.Equal(1, first!.Id);

		var cut = new ContextCheckpoint { Kind = ContextCheckpointKind.Shield };
		var effectiveAfterCut = new EffectiveChatContext([u0, u1], [new EffectiveCheckpoint(cut, -1)], 0);

		var second = stage.Process(agent, effectiveAfterCut);

		Assert.NotNull(second);
		Assert.Equal(2, second!.Id);
		Assert.Same(second, Assert.Single(u1.Message.AdditionalData.GetAll<PromptStateAnchorMessageData>()));
		// The old anchor stays in history inert and untouched (append-only).
		Assert.Same(first, Assert.Single(u0.Message.AdditionalData.GetAll<PromptStateAnchorMessageData>()));
	}

	[Fact]
	public void AgentsDoNotShareAnchors()
	{
		var u0 = PromptingTestHelpers.User("u0", 0);
		var chat = PromptingTestHelpers.CreateChat(u0);
		var stage = CreateStage(chat, new FakeSection(0, "core"));
		var agentA = CreateHybridAgent();
		var agentB = CreateHybridAgent();
		var effective = new EffectiveChatContext([u0], [], -1);

		var anchorA = stage.Process(agentA, effective);
		var anchorB = stage.Process(agentB, effective);

		Assert.NotNull(anchorA);
		Assert.NotNull(anchorB);
		Assert.NotSame(anchorA, anchorB);
		Assert.Equal(agentA.Id, anchorA!.AgentId);
		Assert.Equal(agentB.Id, anchorB!.AgentId);
		var stored = u0.Message.AdditionalData.GetAll<PromptStateAnchorMessageData>();
		Assert.Equal(2, stored.Count);
		Assert.Equal(2, anchorB.Id); // the id counter is chat-wide
	}

	[Fact]
	public void NonHybridMode_ReturnsNull_AndCreatesNothing()
	{
		var u0 = PromptingTestHelpers.User("u0", 0);
		var chat = PromptingTestHelpers.CreateChat(u0);
		var stage = CreateStage(chat, new FakeSection(0, "core"));
		var effective = new EffectiveChatContext([u0], [], -1);

		var anchor = stage.Process(PromptingTestHelpers.CreateAgent(), effective);

		Assert.Null(anchor);
		Assert.Empty(u0.Message.AdditionalData.GetAll<PromptStateAnchorMessageData>());
		Assert.False(chat.AdditionalData.TryGet<PromptStateAnchorIdCounter>(out _));
	}

	[Fact]
	public void EmptyMessages_ReturnsNull()
	{
		var chat = PromptingTestHelpers.CreateChat();
		var stage = CreateStage(chat, new FakeSection(0, "core"));

		var anchor = stage.Process(CreateHybridAgent(), new EffectiveChatContext([], [], -1));

		Assert.Null(anchor);
	}
}
