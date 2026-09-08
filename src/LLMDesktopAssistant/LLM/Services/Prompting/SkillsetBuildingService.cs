using LLMDesktopAssistant.Addons;
using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Prompting.ContextExpanders;
using LLMDesktopAssistant.Prompting.Management;
using LLMDesktopAssistant.Prompting.Plugins;
using LLMDesktopAssistant.Prompting.Skills;
using LLMDesktopAssistant.StructuredValues.Converters;
using LLTSharp;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	[ChatService(typeof(IAddonSetCollector<SkillInfo>))]
	public class SkillsetBuildingService(
		IChatSettingsService chatSettings,
		IPromptSkillManager skillManager,
		IEnumerable<IPromptSystemContextExpander> promptSystemContextExpanders,
		IEnumerable<IPromptTemplatePlugin> promptTemplatePlugins,
		IServiceProvider services
	) : AddonSetCollectorBase<SkillInfo, SkillChange>(services)
	{
		protected override IEnumerable<SkillInfo> GetAdditionalAddons()
		{
			List<SkillInfo> skills = [];

			var promptSkills = skillManager.GetAll().ToList();
			if (promptSkills.Count > 0)
			{
				skills.AddRange(promptSkills.Select(sp =>
				{
					return new SkillInfo
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
						Source = AddonSource.Template,
						TemplateSource = sp.Source,
						ParameterSchema = sp.ParameterSchema
					};
				}));
			}

			return skills;
		}

		protected override void ApplyChange(SkillInfo target, SkillChange change)
		{
			base.ApplyChange(target, change);
			target.InjectionMode = change.InjectionMode ?? target.InjectionMode;
		}

		public override IEnumerable<SkillInfo> GetAddonsForAgent(ChatAgentDescriptor agent)
		{
			if (!chatSettings.Settings.Skills.EnableSkills)
				return [];

			var settings = agent.Skills;
			if (!settings.EnableSkills)
				return [];

			var skillset = settings.GetEffectiveSkillset(chatSettings.Settings);
			return GetAddonsWithChanges(skillset.SkillChanges, skillset.SkillsEnabledByDefault);
		}
	}
}
