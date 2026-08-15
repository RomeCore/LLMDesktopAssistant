using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Prompting;
using LLMDesktopAssistant.Prompting.ContextExpanders;
using LLMDesktopAssistant.Prompting.Plugins;
using LLMDesktopAssistant.Prompting.Skills;
using LLTSharp;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	[ChatService(typeof(ISkillsetBuildingService))]
	public class SkillsetBuildingService(
		IChatSettingsService chatSettings,
		ISkillLocator skillLocator,
		ISkillLoader skillLoader,
		IPromptRegistry promptRegistry,
		IEnumerable<IPromptSystemContextExpander> promptSystemContextExpanders,
		IEnumerable<IPromptTemplatePlugin> promptTemplatePlugins
	) : ISkillsetBuildingService
	{
		public IEnumerable<SkillInfo> GetAvailableSkills()
		{
			var skillFiles = skillLocator.LocateSkillFiles();

			List<SkillInfo> skills = [];

			var promptSkills = promptRegistry.GetSkills().ToList();
			if (promptSkills.Count > 0)
			{
				var context = new Dictionary<string, object?>();
				foreach (var expander in promptSystemContextExpanders)
					expander.ExpandPromptContext(context);
				var templateFunctions = new TemplateFunctionSet(promptTemplatePlugins.SelectMany(p => p.GetTemplateFunctions()));

				skills.AddRange(promptSkills.Select(s =>
				{
					return new SkillInfo
					{
						Name = s.Name,
						Description = s.Description ?? string.Empty,
						BodyGetter = new(() => s.Template.Template.Render(context, templateFunctions))
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
						Path = last.Path,
						HomeDirectory = last.HomeDirectory,
						Metadata = last.Metadata,
						AdditionalMetadata = last.AdditionalMetadata,
						AllowedTools = last.AllowedTools,
						Tags = last.Tags,
						AdditionalProperties = last.AdditionalProperties,
						Enabled = last.Enabled,
						InjectionMode = last.InjectionMode,
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

			var changes = settings.GetEffectiveSkillChanges(chatSettings.Settings).ToDictionary(c => c.SkillName, c => c);
			foreach (var skillInfo in skills)
			{
				if (skillInfo.Diagnostic?.IsFatal == true)
					continue;

				if (changes.TryGetValue(skillInfo.Name, out var change))
				{
					if (change.Enabled ?? skillInfo.Enabled)
						result.Add(new SkillInfo
						{
							Enabled = true,
							InjectionMode = change.InjectionMode ?? skillInfo.InjectionMode,
							Name = skillInfo.Name,
							Description = skillInfo.Description,
							BodyGetter = skillInfo.BodyGetter,
							Path = skillInfo.Path,
							HomeDirectory = skillInfo.HomeDirectory,
							Metadata = skillInfo.Metadata,
							AdditionalMetadata = skillInfo.AdditionalMetadata,
							AllowedTools = skillInfo.AllowedTools,
							Tags = skillInfo.Tags,
							AdditionalProperties = skillInfo.AdditionalProperties,
						});
				}
				else
				{
					if (skillInfo.Enabled)
						result.Add(skillInfo);
				}
			}

			return result;
		}
	}
}
