using System.Text.Json.Nodes;
using LLMDesktopAssistant.Tools;
using LLMDesktopAssistant.Tools.Specifiers;

namespace LLMDesktopAssistant.Tests.Specifiers;

public class SpecifierEngineTests
{
	private static ToolExecutionContext CreateContext()
	{
		var tool = ToolInfo.Create(new ToolInitializationInfo
		{
			Name = "test",
			DescriptionGetter = () => "test",
			Executor = (JsonNode? _, ToolExecutionContext _, CancellationToken _) => Task.FromResult(new ReactiveToolResult())
		});
		return ToolExecutionContext.CreateDummy(tool, null, null);
	}

	private static ToolSpecifierRule Change(string pattern, SpecifierDecision decision) => new()
	{
		Pattern = pattern,
		Decision = decision
	};

	private static Func<Specifier, JsonNode?, ToolExecutionContext, SpecifierMatchResult> Analyzer(params string[] parts)
	{
		return (specifier, _, _) => SpecifierMatcher.Match(specifier, parts, []);
	}

	// ──────────────────────────── Evaluate: basics ────────────────────────────

	[Fact]
	public void Evaluate_NoSpecifiers_None()
	{
		var result = SpecifierEngine.Evaluate([], Analyzer("git status"), null, CreateContext(), SpecifierAggregationMode.Sequential);

		Assert.Equal(SpecifierVerdict.None, result.Verdict);
	}

	[Fact]
	public void Evaluate_NoMatch_None()
	{
		var result = SpecifierEngine.Evaluate([Change("git status:*", SpecifierDecision.Allow)],
			Analyzer("npm install"), null, CreateContext(), SpecifierAggregationMode.Sequential);

		Assert.Equal(SpecifierVerdict.None, result.Verdict);
	}

	[Fact]
	public void Evaluate_AllowWithFullMatch_Allow()
	{
		var result = SpecifierEngine.Evaluate([Change("git status:*", SpecifierDecision.Allow)],
			Analyzer("git status --short"), null, CreateContext(), SpecifierAggregationMode.Sequential);

		Assert.Equal(SpecifierVerdict.Allow, result.Verdict);
		Assert.Contains("git status:*", result.Message);
	}

	[Fact]
	public void Evaluate_AllowWithPartialMatch_None()
	{
		var result = SpecifierEngine.Evaluate([Change("git diff *", SpecifierDecision.Allow)],
			Analyzer("git diff --stat", "git status"), null, CreateContext(), SpecifierAggregationMode.Sequential);

		Assert.Equal(SpecifierVerdict.None, result.Verdict);
	}

	[Fact]
	public void Evaluate_AskWithPartialMatch_Ask()
	{
		var result = SpecifierEngine.Evaluate([Change("git diff *", SpecifierDecision.Ask)],
			Analyzer("git diff --stat", "git status"), null, CreateContext(), SpecifierAggregationMode.Sequential);

		Assert.Equal(SpecifierVerdict.Ask, result.Verdict);
	}

	[Fact]
	public void Evaluate_DenyWithPartialMatch_Deny()
	{
		var result = SpecifierEngine.Evaluate([Change("git diff *", SpecifierDecision.Deny)],
			Analyzer("git diff --stat", "git status"), null, CreateContext(), SpecifierAggregationMode.Sequential);

		Assert.Equal(SpecifierVerdict.Deny, result.Verdict);
	}

	[Fact]
	public void Evaluate_EmptyPattern_Skipped()
	{
		var result = SpecifierEngine.Evaluate([Change("", SpecifierDecision.Deny), Change("git status:*", SpecifierDecision.Allow)],
			Analyzer("git status"), null, CreateContext(), SpecifierAggregationMode.Sequential);

		Assert.Equal(SpecifierVerdict.Allow, result.Verdict);
	}

	[Fact]
	public void Evaluate_InvalidPattern_Skipped()
	{
		var result = SpecifierEngine.Evaluate([Change("git ||", SpecifierDecision.Deny), Change("git status:*", SpecifierDecision.Allow)],
			Analyzer("git status"), null, CreateContext(), SpecifierAggregationMode.Sequential);

		Assert.Equal(SpecifierVerdict.Allow, result.Verdict);
	}

	[Fact]
	public void Evaluate_AnalyzerThrows_Skipped()
	{
		var result = SpecifierEngine.Evaluate([Change("git *", SpecifierDecision.Deny), Change("npm *", SpecifierDecision.Allow)],
			(specifier, _, _) => specifier.Parts.Count == 1 && specifier.Parts[0] is SpecifierLiteralPart { Value: "git *" }
				? throw new InvalidOperationException("boom")
				: SpecifierMatcher.Match(specifier, ["npm install"], []),
			null, CreateContext(), SpecifierAggregationMode.Sequential);

		Assert.Equal(SpecifierVerdict.Allow, result.Verdict);
	}

	// ──────────────────────────── Evaluate: aggregation modes ────────────────────────────

	[Fact]
	public void Evaluate_Sequential_LastMatchWins()
	{
		var result = SpecifierEngine.Evaluate(
			[Change("git status:*", SpecifierDecision.Allow), Change("git *", SpecifierDecision.Ask)],
			Analyzer("git status --short"), null, CreateContext(),
			SpecifierAggregationMode.Sequential);

		Assert.Equal(SpecifierVerdict.Ask, result.Verdict);
	}

	[Fact]
	public void Evaluate_Sequential_NonMatchingBetweenMatches_LastMatchingWins()
	{
		var result = SpecifierEngine.Evaluate(
			[Change("git status:*", SpecifierDecision.Allow), Change("npm *", SpecifierDecision.Deny), Change("git *", SpecifierDecision.Ask)],
			Analyzer("git status --short"), null, CreateContext(),
			SpecifierAggregationMode.Sequential);

		Assert.Equal(SpecifierVerdict.Ask, result.Verdict);
	}

	[Fact]
	public void Evaluate_Prioritized_DenyWinsOverAllowRegardlessOfOrder()
	{
		var result = SpecifierEngine.Evaluate(
			[Change("git *", SpecifierDecision.Allow), Change("git status:*", SpecifierDecision.Deny)],
			Analyzer("git status --short"), null, CreateContext(),
			SpecifierAggregationMode.Prioritized);

		Assert.Equal(SpecifierVerdict.Deny, result.Verdict);
	}

	[Fact]
	public void Evaluate_Prioritized_AskWinsOverAllow()
	{
		var result = SpecifierEngine.Evaluate(
			[Change("git status:*", SpecifierDecision.Ask), Change("git *", SpecifierDecision.Allow)],
			Analyzer("git status --short"), null, CreateContext(),
			SpecifierAggregationMode.Prioritized);

		Assert.Equal(SpecifierVerdict.Ask, result.Verdict);
	}

	[Fact]
	public void Evaluate_Prioritized_AllowWhenNoStricterMatch()
	{
		var result = SpecifierEngine.Evaluate(
			[Change("git status:*", SpecifierDecision.Allow), Change("npm *", SpecifierDecision.Ask)],
			Analyzer("git status --short"), null, CreateContext(),
			SpecifierAggregationMode.Prioritized);

		Assert.Equal(SpecifierVerdict.Allow, result.Verdict);
	}

	[Fact]
	public void Evaluate_AllowPartialMatchDoesNotOverride_Sequential()
	{
		// A partially matched Allow produces no verdict, so the previous verdict stays.
		var result = SpecifierEngine.Evaluate(
			[Change("git diff *", SpecifierDecision.Ask), Change("git status:*", SpecifierDecision.Allow)],
			Analyzer("git diff --stat", "git status"), null, CreateContext(),
			SpecifierAggregationMode.Sequential);

		Assert.Equal(SpecifierVerdict.Ask, result.Verdict);
	}

	// ──────────────────────────── Combine: Disabled ────────────────────────────

	[Theory]
	[InlineData(SpecifierVerdict.Allow, ToolPolicyDecision.Approve, ToolPolicyDecision.Approve)]
	[InlineData(SpecifierVerdict.Allow, ToolPolicyDecision.Ask, ToolPolicyDecision.Ask)]
	[InlineData(SpecifierVerdict.Deny, ToolPolicyDecision.Ask, ToolPolicyDecision.Ask)]
	[InlineData(SpecifierVerdict.None, ToolPolicyDecision.Disallow, ToolPolicyDecision.Disallow)]
	public void Combine_Disabled_ReturnsPolicy(SpecifierVerdict specifier, ToolPolicyDecision policy, ToolPolicyDecision expected)
	{
		Assert.Equal(expected, SpecifierEngine.Combine(specifier, policy, SpecifierBehaviourUnionMode.Disabled));
	}

	// ──────────────────────────── Combine: IgnoreNonSpecifierBehaviours ────────────────────────────

	[Theory]
	[InlineData(SpecifierVerdict.Allow, ToolPolicyDecision.Disallow, ToolPolicyDecision.Approve)]
	[InlineData(SpecifierVerdict.Ask, ToolPolicyDecision.Approve, ToolPolicyDecision.Ask)]
	[InlineData(SpecifierVerdict.Deny, ToolPolicyDecision.Approve, ToolPolicyDecision.Disallow)]
	[InlineData(SpecifierVerdict.None, ToolPolicyDecision.Ask, ToolPolicyDecision.Ask)]
	public void Combine_IgnoreNonSpecifierBehaviours_UsesSpecifier(SpecifierVerdict specifier, ToolPolicyDecision policy, ToolPolicyDecision expected)
	{
		Assert.Equal(expected, SpecifierEngine.Combine(specifier, policy, SpecifierBehaviourUnionMode.IgnoreNonSpecifierBehaviours));
	}

	// ──────────────────────────── Combine: CombineHard ────────────────────────────

	[Theory]
	[InlineData(SpecifierVerdict.Allow, ToolPolicyDecision.Approve, ToolPolicyDecision.Approve)]
	[InlineData(SpecifierVerdict.Allow, ToolPolicyDecision.Ask, ToolPolicyDecision.Ask)]
	[InlineData(SpecifierVerdict.Allow, ToolPolicyDecision.Disallow, ToolPolicyDecision.Disallow)]
	[InlineData(SpecifierVerdict.Ask, ToolPolicyDecision.Approve, ToolPolicyDecision.Ask)]
	[InlineData(SpecifierVerdict.Ask, ToolPolicyDecision.Ask, ToolPolicyDecision.Ask)]
	[InlineData(SpecifierVerdict.Ask, ToolPolicyDecision.Disallow, ToolPolicyDecision.Disallow)]
	[InlineData(SpecifierVerdict.Deny, ToolPolicyDecision.Approve, ToolPolicyDecision.Disallow)]
	[InlineData(SpecifierVerdict.Deny, ToolPolicyDecision.Ask, ToolPolicyDecision.Disallow)]
	[InlineData(SpecifierVerdict.Deny, ToolPolicyDecision.Disallow, ToolPolicyDecision.Disallow)]
	[InlineData(SpecifierVerdict.None, ToolPolicyDecision.Ask, ToolPolicyDecision.Ask)]
	[InlineData(SpecifierVerdict.None, ToolPolicyDecision.Disallow, ToolPolicyDecision.Disallow)]
	public void Combine_CombineHard_StrictestWins(SpecifierVerdict specifier, ToolPolicyDecision policy, ToolPolicyDecision expected)
	{
		Assert.Equal(expected, SpecifierEngine.Combine(specifier, policy, SpecifierBehaviourUnionMode.CombineHard));
	}

	// ──────────────────────────── Combine: CombineSoft ────────────────────────────

	[Theory]
	[InlineData(SpecifierVerdict.Allow, ToolPolicyDecision.Approve, ToolPolicyDecision.Approve)]
	[InlineData(SpecifierVerdict.Allow, ToolPolicyDecision.Ask, ToolPolicyDecision.Approve)]
	[InlineData(SpecifierVerdict.Allow, ToolPolicyDecision.Disallow, ToolPolicyDecision.Disallow)]
	[InlineData(SpecifierVerdict.Ask, ToolPolicyDecision.Approve, ToolPolicyDecision.Ask)]
	[InlineData(SpecifierVerdict.Ask, ToolPolicyDecision.Ask, ToolPolicyDecision.Ask)]
	[InlineData(SpecifierVerdict.Ask, ToolPolicyDecision.Disallow, ToolPolicyDecision.Disallow)]
	[InlineData(SpecifierVerdict.Deny, ToolPolicyDecision.Approve, ToolPolicyDecision.Disallow)]
	[InlineData(SpecifierVerdict.Deny, ToolPolicyDecision.Ask, ToolPolicyDecision.Disallow)]
	[InlineData(SpecifierVerdict.Deny, ToolPolicyDecision.Disallow, ToolPolicyDecision.Disallow)]
	[InlineData(SpecifierVerdict.None, ToolPolicyDecision.Approve, ToolPolicyDecision.Approve)]
	[InlineData(SpecifierVerdict.None, ToolPolicyDecision.Ask, ToolPolicyDecision.Ask)]
	[InlineData(SpecifierVerdict.None, ToolPolicyDecision.Disallow, ToolPolicyDecision.Disallow)]
	public void Combine_CombineSoft_SpecifierSuppressesAskButNotPolicyDisallow(SpecifierVerdict specifier, ToolPolicyDecision policy, ToolPolicyDecision expected)
	{
		Assert.Equal(expected, SpecifierEngine.Combine(specifier, policy, SpecifierBehaviourUnionMode.CombineSoft));
	}
}
