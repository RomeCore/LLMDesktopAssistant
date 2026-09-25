using LLMDesktopAssistant.Agents;
using Serilog;

namespace LLMDesktopAssistant.Prompting.Context
{
	/// <summary>
	/// Merge rules for prompt sections: provider filtering, ordering, state capture and header rendering.
	/// </summary>
	public static class PromptSectionExtensions
	{
		/// <summary>
		/// Filters sections to only include those that are anchored.
		/// </summary>
		public static IEnumerable<IPromptAnchoredSectionProvider> Anchored(this IEnumerable<IPromptContextProvider> sections)
			=> sections.OfType<IPromptAnchoredSectionProvider>();

		/// <summary>
		/// Filters sections to only include those that are supersede.
		/// </summary>
		public static IEnumerable<IPromptSupersedeContextProvider> Supersede(this IEnumerable<IPromptContextProvider> sections)
			=> sections.OfType<IPromptSupersedeContextProvider>();

		/// <summary>
		/// Filters sections to only include those that are live-tail.
		/// </summary>
		public static IEnumerable<IPromptLiveTailContextProvider> LiveTails(this IEnumerable<IPromptContextProvider> sections)
			=> sections.OfType<IPromptLiveTailContextProvider>();

		/// <summary>
		/// Captures the state of every section for the given agent (in section order).
		/// The discriminator of the section is stamped onto the captured state, so that the state
		/// can be matched back to its section later (header rendering and anchor rebaselining).
		/// </summary>
		public static IReadOnlyList<PromptSectionStateBase> CaptureStates(this IEnumerable<IPromptAnchoredSectionProvider> sections,
			ChatAgentDescriptor agent)
		{
			List<PromptSectionStateBase> result = [];

			foreach (var section in sections)
			{
				var state = section.CaptureState(agent);
				if (state == null)
					continue;

				state.Discriminator = section.Discriminator;
				result.Add(state);
			}

			return result;
		}

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
				var state = states.FirstOrDefault(s => s.Discriminator == section.Discriminator);
				if (state == null)
				{
					Log.Debug("No captured state for prompt section {Section} (expected discriminator {Discriminator}).",
						section.GetType().Name, section.Discriminator);
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
