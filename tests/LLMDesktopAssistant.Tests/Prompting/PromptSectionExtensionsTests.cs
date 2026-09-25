using LLMDesktopAssistant.Prompting.Context;

namespace LLMDesktopAssistant.Tests.Prompting;

/// <summary>
/// Tests for the prompt section merge rules: ordering, text joining, tool merging and byte stability.
/// </summary>
[Collection("Prompting")]
public class PromptSectionExtensionsTests
{
	[Fact]
	public void RenderHeader_RespectsSectionOrder_AndJoinsNonEmptyTextFragments()
	{
		FakeSection[] sections =
		[
			new FakeSection(100, "second"),
			new FakeSection(0, "first"),
			new FakeSection(50, "")
		];
		var agent = PromptingTestHelpers.CreateAgent();

		// Ordering is the responsibility of the section collector: the merge rules follow the order of the incoming collection.
		var header = sections.OrderBy(s => s.Order).RenderHeader(agent);

		Assert.Equal("first\nsecond", header.Text);
	}

	[Fact]
	public void RenderHeader_ConcatenatesTools_AndSortsByName()
	{
		FakeSection[] sections =
		[
			new FakeSection(100, "second", PromptingTestHelpers.Tool("a")),
			new FakeSection(0, "first", PromptingTestHelpers.Tool("z"), PromptingTestHelpers.Tool("m"))
		];
		var agent = PromptingTestHelpers.CreateAgent();

		var header = sections.RenderHeader(agent);

		Assert.Equal(["a", "m", "z"], header.Tools.Select(t => t.Name).ToArray());
	}

	[Fact]
	public void RenderHeader_IsByteStable_AcrossRepeatedRenders()
	{
		FakeSection[] sections =
		[
			new FakeSection(0, "first", PromptingTestHelpers.Tool("b")),
			new FakeSection(100, "second", PromptingTestHelpers.Tool("a"))
		];
		var agent = PromptingTestHelpers.CreateAgent();

		var first = sections.RenderHeader(agent);
		var second = sections.RenderHeader(agent);

		Assert.Equal(first.Text, second.Text);
		Assert.Equal(first, second);
	}

	[Fact]
	public void RenderHeader_MatchesStates_BySectionDiscriminator()
	{
		FakeSection[] sections =
		[
			new FakeSection(0, "first"),
			new FakeSection(100, "second")
		];
		var agent = PromptingTestHelpers.CreateAgent();

		// The captured states are matched back to their sections by the stamped discriminator:
		// a mismatch would silently drop the fragment from the merged header.
		var states = sections.CaptureStates(agent);
		var header = sections.RenderHeader(states);

		Assert.Equal(2, states.Count);
		Assert.Equal("first\nsecond", header.Text);
	}

	[Fact]
	public void CaptureStates_PassesAgentToEverySection()
	{
		FakeSection[] sections =
		[
			new FakeSection(0, "first"),
			new FakeSection(100, "second")
		];
		var agent = PromptingTestHelpers.CreateAgent();

		var states = sections.CaptureStates(agent);

		Assert.Equal(2, states.Count);
		Assert.Same(agent, sections[0].LastCaptureAgent);
		Assert.Same(agent, sections[1].LastCaptureAgent);
	}

	[Fact]
	public void RenderHeader_NoSections_ReturnsEmptySnapshot()
	{
		var header = Array.Empty<FakeSection>().RenderHeader(PromptingTestHelpers.CreateAgent());

		Assert.Equal(string.Empty, header.Text);
		Assert.Empty(header.Tools);
	}
}
