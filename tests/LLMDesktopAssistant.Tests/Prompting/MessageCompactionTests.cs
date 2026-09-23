using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Prompting;
using LLMDesktopAssistant.Prompting;

namespace LLMDesktopAssistant.Tests.Prompting;

/// <summary>
/// Tests for the per-message compaction rules derived from effective checkpoints.
/// </summary>
[Collection("Prompting")]
public class MessageCompactionTests
{
	private static EffectiveCheckpoint Checkpoint(ContextCheckpointKind kind, int index)
		=> new(new ContextCheckpoint { Kind = kind }, index);

	[Fact]
	public void NoCheckpoints_NoCompactionRules()
	{
		var rules = MessageCompaction.ForMessage([], 0, ContextCheckpointKind.None);

		Assert.False(rules.Any);
		Assert.False(rules.ShouldCompactToolCall(true));
	}

	[Fact]
	public void ToolCompaction_CoversMessagesUpToItsIndexInclusive()
	{
		var checkpoints = new[] { Checkpoint(ContextCheckpointKind.ToolCompaction, 2) };

		Assert.True(MessageCompaction.ForMessage(checkpoints, 0, ContextCheckpointKind.None).CompactTools);
		Assert.True(MessageCompaction.ForMessage(checkpoints, 2, ContextCheckpointKind.None).CompactTools);
		Assert.False(MessageCompaction.ForMessage(checkpoints, 3, ContextCheckpointKind.None).CompactTools);
	}

	[Fact]
	public void ToolCompaction_RespectsCanBeCompacted()
	{
		var rules = MessageCompaction.ForMessage([Checkpoint(ContextCheckpointKind.ToolCompaction, 0)], 0,
			ContextCheckpointKind.None);

		Assert.True(rules.ShouldCompactToolCall(true));
		Assert.False(rules.ShouldCompactToolCall(false));
	}

	[Fact]
	public void ForcedToolCompaction_IgnoresCanBeCompacted()
	{
		var rules = MessageCompaction.ForMessage([Checkpoint(ContextCheckpointKind.ForcedToolCompaction, 0)], 0,
			ContextCheckpointKind.None);

		Assert.True(rules.ShouldCompactToolCall(false));
	}

	[Fact]
	public void ReasoningCompaction_SetsCompactionFlag()
	{
		var rules = MessageCompaction.ForMessage([Checkpoint(ContextCheckpointKind.ReasoningCompaction, 1)], 1,
			ContextCheckpointKind.None);

		Assert.True(rules.CompactReasoning);
	}

	[Fact]
	public void DisabledFlags_MaskOutBits()
	{
		var checkpoints = new[]
		{
			Checkpoint(ContextCheckpointKind.ToolCompaction | ContextCheckpointKind.ReasoningCompaction, 0)
		};

		var rules = MessageCompaction.ForMessage(checkpoints, 0, ContextCheckpointKind.ToolCompaction);

		Assert.False(rules.CompactTools);
		Assert.True(rules.CompactReasoning);
	}

	[Fact]
	public void CutOnlyCheckpoint_NeverCovers()
	{
		var checkpoints = new[]
		{
			Checkpoint(ContextCheckpointKind.Shield | ContextCheckpointKind.Summary, -1)
		};

		var rules = MessageCompaction.ForMessage(checkpoints, 0, ContextCheckpointKind.None);

		Assert.False(rules.Any);
	}

	[Fact]
	public void Placeholders_AreStatusAware()
	{
		Assert.Contains("SUCCESSFUL", MessageCompaction.GetCompactedToolResultContent(ToolStatus.Success));
		Assert.Contains("FAULTED", MessageCompaction.GetCompactedToolResultContent(ToolStatus.Error));
		Assert.Contains("CANCELLED", MessageCompaction.GetCompactedToolResultContent(ToolStatus.Cancelled));
	}
}
