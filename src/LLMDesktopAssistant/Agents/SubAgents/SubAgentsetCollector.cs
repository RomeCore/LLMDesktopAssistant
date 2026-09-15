using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Prompting.ContextExpanders;
using LLMDesktopAssistant.Prompting.Management;
using LLMDesktopAssistant.Prompting.Plugins;
using LLMDesktopAssistant.StructuredValues.Converters;
using LLTSharp;

namespace LLMDesktopAssistant.Agents.SubAgents
{
	/// <summary>
	/// The collector that builds the effective sub-agent addon set for the chat and its agents:
	/// file-system sub-agents (from addon packs and additional sources) plus prompt template sub-agents.
	/// </summary>
	[ChatService(typeof(IAddonSetCollector<SubAgentInfo>))]
	public class SubAgentsetCollector(
		IChatSettingsService chatSettings,
		IPromptSubAgentManager subAgentManager,
		IEnumerable<IPromptSystemContextExpander> promptSystemContextExpanders,
		IEnumerable<IPromptTemplatePlugin> promptTemplatePlugins,
		IServiceProvider services
	) : AddonSetCollectorBase<SubAgentInfo, SubAgentChange>(services)
	{
		/// <remarks>
		/// File-system sub-agents are added after the template ones, so they override template sub-agents
		/// with the same name (the last addon in a name group wins and the rest become overrides).
		/// </remarks>
		protected override bool AdditionalGoingFirst => true;

		protected override IEnumerable<SubAgentInfo> GetAdditionalAddons()
		{
			List<SubAgentInfo> subAgents = [];

			var promptSubAgents = subAgentManager.GetAll().ToList();
			if (promptSubAgents.Count > 0)
			{
				subAgents.AddRange(promptSubAgents.Select(sp =>
				{
					return new SubAgentInfo
					{
						Name = sp.Name,
						Description = sp.Description ?? string.Empty,
						BodyGetter = new(si =>
						{
							var context = new Dictionary<string, object?>();
							foreach (var expander in promptSystemContextExpanders)
								expander.ExpandPromptContext(context);
							var templateFunctions = new TemplateFunctionSet(promptTemplatePlugins.SelectMany(p => p.GetTemplateFunctions()));

							if (sp.ParameterSchema is not null)
							{
								if (si.Change is not null)
								{
									si.Change.Parameters = sp.ParameterSchema.Root.CreateOrFixValue(si.Change.Parameters, []);
									context["params"] = LLTStructuredConverter.ToTemplateDataAccessor(si.Change.Parameters);
								}
								else
								{
									var @params = sp.ParameterSchema.Root.CreateOrFixValue(null, []);
									context["params"] = LLTStructuredConverter.ToTemplateDataAccessor(@params);
								}
							}
							return sp.EffectiveTemplate.Render(context, templateFunctions).ToString() ?? string.Empty;
						}),
						AddonSource = AddonSource.Template,
						TemplateSource = sp.Source,
						ParameterSchema = sp.ParameterSchema
					};
				}));
			}

			return subAgents;
		}

		protected override void ApplyChange(SubAgentInfo target, SubAgentChange change, ChatAgentDescriptor? agent)
		{
			base.ApplyChange(target, change, agent);
			target.Model = change.Model ?? target.Model;
		}

		public override IEnumerable<SubAgentInfo> GetAddonsForAgent(ChatAgentDescriptor agent)
		{
			var settings = agent.SubAgents;
			if (!settings.EnableSubAgents)
				return [];

			var subAgentset = settings.GetEffectiveSubAgentset(chatSettings.Settings);
			return GetAddonsWithChanges(subAgentset, agent);
		}
	}
}
