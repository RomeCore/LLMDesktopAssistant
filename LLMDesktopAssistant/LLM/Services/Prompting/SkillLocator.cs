using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	[ChatService(typeof(ISkillLocator))]
	public class SkillLocator(
		Chat chat
	) : ISkillLocator
	{
		public IEnumerable<string> LocateSkillFiles()
		{
			// 1. Skill directories
			List<string> potentialSkillDirectories = [];

			potentialSkillDirectories.AddRange(chat.Settings.Skills.AdditionalSkillDirectories);

			string[] projectPaths;
			if (chat.Settings.Skills.FetchFromAllWorkingDirectories)
				projectPaths = [..chat.Settings.Environment.GetEffectiveWorkingDirectories().GetEnabledWorkingDirectories()];
			else
				projectPaths = [chat.Settings.Environment.GetEffectiveWorkingDirectories().GetWorkingDirectory()];

			List<string> sharedCheckPaths = [
				$"{Directories.WorkingHome}/skills", // .llmassist/skills
				".agents/skills",
				".claude/skills",
				".github/skills",
				".codex/skills",
				".cursor/skills"
			];

			var sharedRootFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			foreach (string sharedCheckPath in sharedCheckPaths)
			{
				var potentialSkillDirectory = Path.Combine(sharedRootFolder, sharedCheckPath);
				potentialSkillDirectories.Add(potentialSkillDirectory);
			}

			List<string> projectCheckPaths = [];
			projectCheckPaths.AddRange(sharedCheckPaths);
			foreach (string projectPath in projectPaths)
			{
				foreach (string checkPath in projectCheckPaths)
				{
					var potentialSkillDirectory = Path.Combine(projectPath, checkPath);
					potentialSkillDirectories.Add(potentialSkillDirectory);
				}
			}

			// 2. Skill files

			List<string> skillFiles = [];

			skillFiles.AddRange(chat.Settings.Skills.AdditionalSkillFiles);

			foreach (string potentialSkillDirectory in potentialSkillDirectories)
			{
				if (!Directory.Exists(potentialSkillDirectory))
					continue;

				var subdirectories = Directory.GetDirectories(potentialSkillDirectory);
				foreach (string subdirectory in subdirectories)
				{
					skillFiles.Add(Path.Combine(subdirectory, "SKILL.md"));
					skillFiles.Add(Path.Combine(subdirectory, "SKILL.mdx"));
				}
			}

			var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
			return skillFiles.Distinct(comparer).Where(File.Exists);
		}
	}
}
