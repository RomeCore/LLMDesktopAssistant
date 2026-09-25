using LLMDesktopAssistant.Agents;
using Serilog;

namespace LLMDesktopAssistant.Prompting.Context
{
	/// <summary>
	/// Merge rules for prompt sections: ordering, state capture and header rendering.
	/// </summary>
	public static class PromptSectionExtensions
	{
		/// <summary>
		/// Filters sections to only include those that are anchored.
		/// </summary>
		public static IEnumerable<IPromptAnchoredSectionProvider> Anchored(this IEnumerable<IPromptContextProvider> sections)
			=> sections.OfType<IPromptAnchoredSectionProvider>();

		/// <summary>
		/// Captures the state of every section for the given agent (in section order).
		/// </summary>
		public static IReadOnlyList<PromptSectionStateBase> CaptureStates(this IEnumerable<IPromptAnchoredSectionProvider> sections,
			ChatAgentDescriptor agent)
			=> sections.Select(s => s.CaptureState(agent)!).Where(s => s != null).ToList();

		/// <summary>
		/// Renders the merged system prompt header from the captured states:
		/// non-empty text fragments are joined with a single newline, tools are concatenated and sorted by name.
		/// </summary>
		public static SystemPromptSnapshot RenderHeader(this IEnumerable<IPromptAnchoredSectionProvider> sections,
			IReadOnlyList<PromptSectionStateBase> states)
		{
			var texts = new List<string>();
			var tools = new List<SerializableToolDefinition>();

			foreach (var section in sections)
			{
				var state = states.FirstOrDefault(s => s.GetType() == section.StateType);
				if (state == null)
				{
					Log.Debug("No captured state for prompt section {Section} (expected state type {StateType}).",
						section.GetType().Name, section.StateType.Name);
					continue;
				}

				var snapshot = section.RenderState(state);
				if (!string.IsNullOrEmpty(snapshot.Text))
					texts.Add(snapshot.Text);
				if (snapshot.Tools.Count > 0)
					tools.AddRange(snapshot.Tools);
			}

			return new SystemPromptSnapshot
			{
				Text = string.Join("\n", texts),
				Tools = [.. tools.OrderBy(t => t.Name, StringComparer.Ordinal)]
			};
		}

		/// <summary>
		/// Captures and renders the merged system prompt header for the given agent.
		/// </summary>
		public static SystemPromptSnapshot RenderHeader(this IEnumerable<IPromptAnchoredSectionProvider> sections,
			ChatAgentDescriptor agent)
			=> sections.RenderHeader(sections.CaptureStates(agent));
	}
}
