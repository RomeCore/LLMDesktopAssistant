using System.Text.RegularExpressions;
using YamlDotNet.RepresentationModel;

namespace LLMDesktopAssistant.Tools.Meta
{
	public static partial class MetaToolHumanizedEnumNames
	{
		[GeneratedRegex(@"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", RegexOptions.Compiled)]
		private static partial Regex GetKebabCaseRegex();
		private static readonly Regex _kebabCaseRegex = GetKebabCaseRegex();

		public static readonly ImmutableDictionary<ToolApprovalLevel, string> ApprovalLevelNames = Enum.GetValues<ToolApprovalLevel>()
			.Select(level => (level, ToKebabCase(level.ToString())))
			.ToImmutableDictionary(k => k.level, v => v.Item2);

		public static readonly ImmutableDictionary<ToolBehaviour, string> BehaviourNames = Enum.GetValues<ToolBehaviour>()
			.Where(IsSingleFlag)
			.Select(flag => (flag, ToKebabCase(flag.ToString())))
			.ToImmutableDictionary(k => k.flag, v => v.Item2);

		private static bool IsSingleFlag(ToolBehaviour behaviour)
		{
			var value = (long)behaviour;
			return value != 0 && (value & (value - 1)) == 0;
		}

		private static string ToKebabCase(string name)
		{
			return _kebabCaseRegex.Replace(name, "-").ToLowerInvariant();
		}

		public static ToolApprovalLevel DeserializeApprovalLevel(string approvalLevel)
		{
			var normalized = approvalLevel.Trim().ToLowerInvariant().Replace('_', '-');

			foreach (var (level, name) in ApprovalLevelNames)
				if (name == normalized)
					return level;

			if (Enum.TryParse<ToolApprovalLevel>(approvalLevel, ignoreCase: true, out var parsed))
				return parsed;

			return ToolApprovalLevel.PolicyBased;
		}

		public static ToolBehaviour DeserializeToolBehaviour(string behaviour)
		{
			var normalized = behaviour.Trim().ToLowerInvariant().Replace('_', '-');

			foreach (var (flag, name) in BehaviourNames)
				if (name == normalized)
					return flag;

			if (Enum.TryParse<ToolBehaviour>(behaviour, ignoreCase: true, out var parsed))
				return parsed;

			return ToolBehaviour.None;
		}

		public static ToolBehaviour ResolveBehaviours(string[]? behaviors)
		{
			var result = ToolBehaviour.None;
			foreach (var behaviour in behaviors ?? [])
				result |= DeserializeToolBehaviour(behaviour);
			return result;
		}

		public static string[]? SerializeBehaviours(ToolBehaviour behaviours)
		{
			if (behaviours == ToolBehaviour.None)
				return null;

			var result = new List<string>();
			foreach (var (flag, name) in BehaviourNames)
				if (behaviours.HasFlag(flag))
					result.Add(name);
			return result.ToArray();
		}

		public static string SerializeApprovalLevel(ToolApprovalLevel level)
		{
			foreach (var (itemLevel, name) in ApprovalLevelNames)
				if (itemLevel == level)
					return name;

			return "policy-based";
		}
	}
}
