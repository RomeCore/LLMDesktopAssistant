using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Prompting.ContextExpanders;
using LLMDesktopAssistant.Prompting.Management;
using LLMDesktopAssistant.Prompting.Plugins;
using LLMDesktopAssistant.Prompting.Skills;
using LLMDesktopAssistant.StructuredValues.Converters;
using LLTSharp;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	[ChatService(typeof(ISkillsetBuildingService))]
	public class SkillsetBuildingService(
		IChatSettingsService chatSettings,
		ISkillLocator skillLocator,
		ISkillLoader skillLoader,
		IPromptSkillManager skillManager,
		IEnumerable<IPromptSystemContextExpander> promptSystemContextExpanders,
		IEnumerable<IPromptTemplatePlugin> promptTemplatePlugins
	) : ISkillsetBuildingService
	{
		public IEnumerable<SkillInfo> GetAvailableSkills()
		{
			var skillFiles = skillLocator.LocateSkillFiles();

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
						Source = SkillSource.Template,
						TemplateSource = sp.Source,
						ParameterSchema = sp.ParameterSchema
					};
				}));
			}
			skills.AddRange(skillLoader.Load(skillFiles));

			return skills
				.GroupBy(s => s.Name)
				.Select(g =>
				{
					ImmutableList<SkillInfo>.Builder? overridesBuilder = null;
					SkillInfo? last = null;
					foreach (var skill in g)
					{
						if (last is not null)
						{
							overridesBuilder ??= ImmutableList.CreateBuilder<SkillInfo>();
							overridesBuilder.Add(last);
						}
						last = skill;
					}
					if (overridesBuilder == null)
						return last!;
					return new SkillInfo
					{
						Name = last!.Name,
						Description = last.Description,
						BodyGetter = last.BodyGetter,
						Source = last.Source,
						TemplateSource = last.TemplateSource,
						Path = last.Path,
						HomeDirectory = last.HomeDirectory,
						Metadata = last.Metadata,
						AdditionalMetadata = last.AdditionalMetadata,
						AllowedTools = last.AllowedTools,
						Tags = last.Tags,
						AdditionalProperties = last.AdditionalProperties,
						Enabled = last.Enabled,
						InjectionMode = last.InjectionMode,
						ParameterSchema = last.ParameterSchema,
						Overrides = overridesBuilder.ToImmutable()
					};
				});
		}

		public IEnumerable<SkillInfo> GetSkillsForAgent(ChatAgentDescriptor agent)
		{
			if (!chatSettings.Settings.Skills.EnableSkills)
				return [];

			var settings = agent.Skills;
			if (!settings.EnableSkills)
				return [];

			var skills = GetAvailableSkills();
			var result = new List<SkillInfo>();

			var skillset = settings.GetEffectiveSkillset(chatSettings.Settings);

			var changes = skillset.SkillChanges.ToDictionary(c => c.SkillName, c => c);
			foreach (var skillInfo in skills)
			{
				if (skillInfo.Diagnostic?.IsFatal == true)
					continue;

				if (changes.TryGetValue(skillInfo.Name, out var change))
				{
					if (change.Enabled ?? skillInfo.Enabled ?? skillset.SkillsEnabledByDefault)
						result.Add(new SkillInfo
						{
							Name = skillInfo.Name,
							Source = skillInfo.Source,
							TemplateSource = skillInfo.TemplateSource,
							Description = skillInfo.Description,
							BodyGetter = skillInfo.BodyGetter,
							Path = skillInfo.Path,
							HomeDirectory = skillInfo.HomeDirectory,
							Metadata = skillInfo.Metadata,
							AdditionalMetadata = skillInfo.AdditionalMetadata,
							AllowedTools = skillInfo.AllowedTools,
							Tags = skillInfo.Tags,
							AdditionalProperties = skillInfo.AdditionalProperties,
							Enabled = true,
							InjectionMode = change.InjectionMode ?? skillInfo.InjectionMode,
							Change = change,
							ParameterSchema = skillInfo.ParameterSchema,
							Overrides = skillInfo.Overrides
						});
				}
				else
				{
					if (skillInfo.Enabled ?? skillset.SkillsEnabledByDefault)
						result.Add(skillInfo);
				}
			}

			return result;
		}
	}
}
