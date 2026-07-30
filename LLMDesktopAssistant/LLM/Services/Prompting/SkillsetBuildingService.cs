using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Prompting.Skills;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	[ChatService(typeof(ISkillsetBuildingService))]
	public class SkillsetBuildingService(
		Chat chat,
		ISkillLocator skillLocator,
		ISkillLoader skillLoader
	) : ISkillsetBuildingService
	{
		public IEnumerable<SkillInfo> GetAvailableSkills()
		{
			return skillLoader.Load(skillLocator.LocateSkillFiles());
		}

		public IEnumerable<SkillInfo> GetSkillsForAgent(ChatAgentDescriptor agent)
		{
			if (!chat.Settings.Skills.EnableSkills)
				return [];

			var settings = agent.Skills;
			if (!settings.EnableSkills)
				return [];

			var skills = GetAvailableSkills();
			var result = new List<SkillInfo>();

			var changes = settings.SkillChanges.ToDictionary(c => c.SkillName, c => c);
			foreach (var skillInfo in skills)
			{
				if (changes.TryGetValue(skillInfo.Name, out var change))
				{
					if (change.Enabled ?? skillInfo.Enabled)
						result.Add(new SkillInfo
						{
							Enabled = true,
							InjectionMode = change.InjectionMode ?? skillInfo.InjectionMode,
							Name = skillInfo.Name,
							Description = skillInfo.Description,
							Body = skillInfo.Body,
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

			return result
				.GroupBy(s => s.Name)
				.Select(g => g.Last());
		}
	}
}
