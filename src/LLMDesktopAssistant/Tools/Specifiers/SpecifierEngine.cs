using System.Text.Json.Nodes;

namespace LLMDesktopAssistant.Tools.Specifiers;

/// <summary>
/// Aggregates the verdicts of tool specifiers and combines them with the policy decision
/// of the standard approval pipeline.
/// </summary>
public static class SpecifierEngine
{
	/// <summary>
	/// Evaluates the specifier rules of a tool against its arguments and aggregates their verdicts.
	/// <list type="bullet">
	/// <item><description><see cref="SpecifierDecision.Allow"/> requires
	/// <see cref="SpecifierMatchResult.FullMatch"/>; a partial match is ignored.</description></item>
	/// <item><description><see cref="SpecifierDecision.Ask"/> and <see cref="SpecifierDecision.Deny"/>
	/// require any match (<see cref="SpecifierMatchResult.PartialMatch"/> or
	/// <see cref="SpecifierMatchResult.FullMatch"/>).</description></item>
	/// </list>
	/// When no specifier matches, <see cref="SpecifierVerdict.None"/> is returned and the standard
	/// policy decides.
	/// </summary>
	/// <param name="specifiers">The specifier rules of the tool. Cannot be <see langword="null"/>.</param>
	/// <param name="analyzer">The specifier analyzer of the tool. Cannot be <see langword="null"/>.</param>
	/// <param name="args">The tool arguments, or <see langword="null"/>.</param>
	/// <param name="context">The tool execution context. Cannot be <see langword="null"/>.</param>
	/// <param name="specifierParameters">The names of the tool parameters that can be referenced
	/// in specifier patterns. Parameter references (<c>name:value</c>) in patterns are recognized
	/// only when the name is listed here; otherwise they are treated as plain literals.</param>
	/// <param name="mode">The aggregation mode.</param>
	/// <returns>The aggregated verdict and the message describing the matched specifier.</returns>
	public static SpecifierVerdictResult Evaluate(
		IEnumerable<ToolSpecifierRule> specifiers,
		Func<Specifier, JsonNode?, ToolExecutionContext, SpecifierMatchResult> analyzer,
		JsonNode? args,
		ToolExecutionContext context,
		IEnumerable<string> specifierParameters,
		SpecifierAggregationMode mode)
	{
		ArgumentNullException.ThrowIfNull(specifiers);
		ArgumentNullException.ThrowIfNull(analyzer);
		ArgumentNullException.ThrowIfNull(context);

		var verdict = SpecifierVerdict.None;
		string message = string.Empty;

		foreach (var change in specifiers)
		{
			if (string.IsNullOrWhiteSpace(change.Pattern))
				continue;

			if (SpecifierParser.TryParse(change.Pattern, specifierParameters) is not { } specifier)
				continue;

			SpecifierMatchResult match;
			try
			{
				match = analyzer(specifier, args, context);
			}
			catch (Exception ex)
			{
				Serilog.Log.Debug(ex, "Error analyzing specifier '{Pattern}': {ErrorMessage}", change.Pattern, ex.Message);
				continue;
			}

			if (match == SpecifierMatchResult.NoMatch)
				continue;

			var current = change.Decision switch
			{
				SpecifierDecision.Allow => match == SpecifierMatchResult.FullMatch ? SpecifierVerdict.Allow : SpecifierVerdict.None,
				SpecifierDecision.Ask => SpecifierVerdict.Ask,
				SpecifierDecision.Deny => SpecifierVerdict.Deny,
				_ => SpecifierVerdict.None
			};

			if (current == SpecifierVerdict.None)
				continue;

			switch (mode)
			{
				case SpecifierAggregationMode.Sequential:
					verdict = current;
					message = $"Specifier '{change.Pattern}' matched with decision {change.Decision}.";
					break;

				case SpecifierAggregationMode.Prioritized:
					if (current > verdict)
					{
						verdict = current;
						message = $"Specifier '{change.Pattern}' matched with decision {change.Decision}.";
					}
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid specifier aggregation mode.");
			}
		}

		return new SpecifierVerdictResult(verdict, message);
	}

	/// <summary>
	/// Combines the specifier verdict with the policy decision according to the union mode.
	/// The default mode (<see cref="SpecifierBehaviourUnionMode.CombineSoft"/>) makes the specifier
	/// the higher layer: an <see cref="SpecifierVerdict.Allow"/> suppresses the policy
	/// <see cref="ToolPolicyDecision.Ask"/>, while the policy <see cref="ToolPolicyDecision.Disallow"/>
	/// remains untouchable.
	/// </summary>
	/// <param name="specifierVerdict">The verdict of the specifier layer.</param>
	/// <param name="policyDecision">The decision of the standard approval pipeline.</param>
	/// <param name="mode">The union mode.</param>
	/// <returns>The combined tool policy decision.</returns>
	public static ToolPolicyDecision Combine(
		SpecifierVerdict specifierVerdict,
		ToolPolicyDecision policyDecision,
		SpecifierBehaviourUnionMode mode)
	{
		switch (mode)
		{
			case SpecifierBehaviourUnionMode.Disabled:
				return policyDecision;

			case SpecifierBehaviourUnionMode.IgnoreNonSpecifierBehaviours:
				return specifierVerdict switch
				{
					SpecifierVerdict.Allow => ToolPolicyDecision.Approve,
					SpecifierVerdict.Ask => ToolPolicyDecision.Ask,
					SpecifierVerdict.Deny => ToolPolicyDecision.Disallow,
					_ => policyDecision
				};

			case SpecifierBehaviourUnionMode.CombineHard:
				return Max(specifierVerdict, policyDecision);

			case SpecifierBehaviourUnionMode.CombineSoft:
				if (specifierVerdict == SpecifierVerdict.Deny)
					return ToolPolicyDecision.Disallow;
				if (policyDecision == ToolPolicyDecision.Disallow)
					return ToolPolicyDecision.Disallow;
				if (specifierVerdict == SpecifierVerdict.Allow)
					return ToolPolicyDecision.Approve;
				if (specifierVerdict == SpecifierVerdict.Ask)
					return ToolPolicyDecision.Ask;
				return policyDecision;

			default:
				throw new ArgumentOutOfRangeException(nameof(mode), mode, "Invalid specifier behaviour union mode.");
		}
	}

	/// <summary>
	/// Returns the strictest of the specifier verdict and the policy decision
	/// (Deny &gt; Ask &gt; Approve), treating <see cref="SpecifierVerdict.None"/> as neutral.
	/// </summary>
	private static ToolPolicyDecision Max(SpecifierVerdict specifierVerdict, ToolPolicyDecision policyDecision)
	{
		int specifierStrictness = specifierVerdict switch
		{
			SpecifierVerdict.Allow => 1,
			SpecifierVerdict.Ask => 2,
			SpecifierVerdict.Deny => 3,
			_ => 0
		};
		int policyStrictness = policyDecision switch
		{
			ToolPolicyDecision.Approve => 1,
			ToolPolicyDecision.Ask => 2,
			ToolPolicyDecision.Disallow => 3,
			_ => 0
		};

		return specifierStrictness >= policyStrictness
			? specifierVerdict switch
			{
				SpecifierVerdict.Allow => ToolPolicyDecision.Approve,
				SpecifierVerdict.Ask => ToolPolicyDecision.Ask,
				SpecifierVerdict.Deny => ToolPolicyDecision.Disallow,
				_ => policyDecision
			}
			: policyDecision;
	}
}
