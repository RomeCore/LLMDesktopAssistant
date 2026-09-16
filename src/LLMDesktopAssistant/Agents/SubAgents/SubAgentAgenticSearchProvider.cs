using System.Text;
using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Addons.Search;
using LLMDesktopAssistant.LLM.Services;

namespace LLMDesktopAssistant.Agents.SubAgents
{
	/// <summary>
	/// The 'addon-search' provider for sub-agents: renders the sub-agent name, description and, in the
	/// detailed mode, the model override and the common addon metadata.
	/// </summary>
	[ChatService(typeof(IAddonAgenticSearchProvider))]
	public class SubAgentAgenticSearchProvider(
		IAddonSetCollector<SubAgentInfo> collector,
		IAddonSearchService<SubAgentInfo> searchService)
		: AddonAgenticSearchProvider<SubAgentInfo>(collector, searchService)
	{
		/// <inheritdoc/>
		public override AddonKind Kind => AddonKind.SubAgent;

		/// <inheritdoc/>
		public override string Title => "Sub-agents";

		/// <inheritdoc/>
		public override string UsageHint => "call with `agent-callsub` (`agentName`) or pass to `agent-call*` tools via `allowedSubAgents`";

		/// <inheritdoc/>
		protected override void AppendAddon(StringBuilder builder, SubAgentInfo subAgent, bool detailed)
		{
			AddonSearchFormatting.AppendItem(builder, subAgent.Name, subAgent.Description);

			if (!detailed)
				return;

			if (!string.IsNullOrEmpty(subAgent.Model))
				builder.Append("  - model: ").AppendLine(subAgent.Model);

			AddonSearchFormatting.AppendMetadata(builder, subAgent.Tags, subAgent.SourcePack?.Name, subAgent.HomeDirectory);
		}
	}
}
