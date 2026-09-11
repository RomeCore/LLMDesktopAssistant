using AngleSharp.Common;
using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Tools;

namespace LLMDesktopAssistant.LLM.Services.Tools
{
	/// <summary>
	/// The default implementation of the <see cref="IToolsetBuildingService"/> interface.
	/// </summary>
	[ChatService(typeof(IAddonSetCollector<ToolInfo>))]
	public class ToolsetBuildingService(
		Chat chat,
		IChatSettingsService chatSettings,
		IMCPManagementService mcpManager,
		IServiceProvider services
		) : AddonSetCollectorBase<ToolInfo, ToolChange>(services)
	{
		private readonly IServiceProvider _services = services;

		protected override IEnumerable<ToolInfo> GetAdditionalAddons()
		{
			return _services.GetServices<ToolModule>()
				.Concat(chat.AdditionalTools ?? [])
				.Concat(mcpManager.GetMCPTools())
				.SelectMany(m => m.GetTools());
		}

		public override IEnumerable<ToolInfo> GetAddonsForAgent(ChatAgentDescriptor agent)
		{
			var settings = agent.Tools;
			if (!settings.EnableTools)
				return [];

			var toolset = settings.GetEffectiveToolset(chatSettings.Settings).GetEffectiveConfiguration();
			return GetAddonsWithChanges(toolset.ToolChanges, agent, toolset.ToolsEnabledByDefault, toolset.ToolsHiddenByDefault);
		}

		protected override void ApplyChange(ToolInfo target, ToolChange change, ChatAgentDescriptor agent)
		{
			base.ApplyChange(target, change, agent);
			target.ApprovalLevel = change.ApprovalLevel ?? target.ApprovalLevel ??
				agent.Tools.GetEffectiveToolset(chatSettings.Settings).GetEffectiveConfiguration().DefaultApprovalLevel;
			target.PolicyMask = change.PolicyMask ?? target.PolicyMask;
			target.SpecifierUnionMode = change.SpecifierUnionMode ?? target.SpecifierUnionMode;
			target.SpecifierAggregationMode = change.SpecifierAggregationMode ?? target.SpecifierAggregationMode;
			target.SpecifierParameters = target.SpecifierParameters;
			target.Specifiers = [.. change.Specifiers];
		}
	}
}