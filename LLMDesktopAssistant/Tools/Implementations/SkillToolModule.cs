using System.ComponentModel;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Services.Agents;
using LLMDesktopAssistant.LLM.Services.Prompting;
using Material.Icons;

namespace LLMDesktopAssistant.Tools.Implementations
{
	[ToolModule]
	public class SkillToolModule : ToolModule
	{
		private readonly Chat _chat;
		private readonly IAgentManagementService _agentManager;
		private readonly ISkillsetBuildingService _skillsetBuilder;

		public SkillToolModule(Chat chat, IAgentManagementService agentManager,
			ISkillsetBuildingService skillsetBuilder)
		{
			_chat = chat;
			_agentManager = agentManager;
			_skillsetBuilder = skillsetBuilder;

			AddTool(LoadSkill, new ToolInitializationInfo
			{
				Name = "skill-load",
				IsFixed = true,
				Description = "Loads a skill (SKILL.md format) by its name.",
				Category = "skills",
				DefaultExpectedBehaviour = ToolBehaviour.None
			});
		}

		public override IEnumerable<ToolInfo> GetTools()
		{
			if (!_chat.Settings.Skills.EnableSkills)
				return [];
			return base.GetTools();
		}

		private ReactiveToolResult LoadSkill(
			[Description("The name of the skill to load.")] string name,
			ToolExecutionContext ctx)
		{
			var senderAgent = _agentManager.GetAgentDescriptor(ctx.Message.SenderAgentId);
			var skills = _skillsetBuilder.GetSkillsForAgent(senderAgent);
			var foundSkill = skills.FirstOrDefault(s => s.Name == name);

			if (foundSkill == null)
			{
				return new ReactiveToolResult
				{
					StatusIcon = MaterialIconKind.Cards,
					StatusTitle = $"*{name}*",
					ResultContent = $"No skill found with the name *{name}*.",
					UseMarkdown = true
				}.CompleteWithError();
			}

			var body = foundSkill.BodyGetter();

			return new ReactiveToolResult
			{
				StatusIcon = MaterialIconKind.Cards,
				StatusTitle = $"*{name}*",
				ResultContent = string.IsNullOrEmpty(foundSkill.HomeDirectory) ? body : $"""
				{body}

				---

				**Note**: all paths in the skill are relative to skill's home path: *{foundSkill.HomeDirectory}*
				""",
				UseMarkdown = true
			}.CompleteWithSuccess();
		}
	}
}
