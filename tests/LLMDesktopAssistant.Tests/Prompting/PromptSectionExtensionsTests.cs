using LLMDesktopAssistant.Prompting.State;

namespace LLMDesktopAssistant.Tests.Prompting;

/// <summary>
/// Tests for the prompt section merge rules: ordering, text joining, tool merging and byte stability.
/// </summary>
[Collection("Prompting")]
public class PromptSectionExtensionsTests
{
	[Fact]
	public void RenderHeader_OrdersSections_AndJoinsNonEmptyTextFragments()
	{
		var sections = new IPromptSection[]
		{
			new FakeSection(100, "second"),
			new FakeSection(0, "first"),
			new FakeSection(50, "")
		};
		var agent = PromptingTestHelpers.CreateAgent();

		var header = sections.RenderHeader(agent);

		Assert.Equal("first\nsecond", header.Text);
	}

	[Fact]
	public void RenderHeader_ConcatenatesTools_AndSortsByName()
	{
		var sections = new IPromptSection[]
		{
			new FakeSection(100, "second", PromptingTestHelpers.Tool("a")),
			new FakeSection(0, "first", PromptingTestHelpers.Tool("z"), PromptingTestHelpers.Tool("m"))
		};
		var agent = PromptingTestHelpers.CreateAgent();

		var header = sections.RenderHeader(agent);

		Assert.Equal(["a", "m", "z"], header.Tools.Select(t => t.Name).ToArray());
	}

	[Fact]
	public void RenderHeader_IsByteStable_AcrossRepeatedRenders()
	{
		var sections = new IPromptSection[]
		{
			new FakeSection(0, "first", PromptingTestHelpers.Tool("b")),
			new FakeSection(100, "second", PromptingTestHelpers.Tool("a"))
		};
		var agent = PromptingTestHelpers.CreateAgent();

		var first = sections.RenderHeader(agent);
		var second = sections.RenderHeader(agent);

		Assert.Equal(first.Text, second.Text);
		Assert.Equal(first, second);
	}

	[Fact]
	public void CaptureStates_PassesAgentToEverySection()
	{
		var first = new FakeSection(0, "first");
		var second = new FakeSection(100, "second");
		var sections = new IPromptSection[] { first, second };
		var agent = PromptingTestHelpers.CreateAgent();

		var states = sections.CaptureStates(agent);

		Assert.Equal(2, states.Count);
		Assert.Same(agent, first.LastCaptureAgent);
		Assert.Same(agent, second.LastCaptureAgent);
	}

	[Fact]
	public void RenderHeader_NoSections_ReturnsEmptySnapshot()
	{
		var header = Array.Empty<IPromptSection>().RenderHeader(PromptingTestHelpers.CreateAgent());

		Assert.Equal(string.Empty, header.Text);
		Assert.Empty(header.Tools);
	}
}
