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
			string[] projectPaths;
			if (chat.Settings.Skills.FetchFromAllWorkingDirectories)
				projectPaths = chat.Settings.Environment.WorkingDirectories
					.Append(new WorkingDirectorySetting
					{
						IsEnabled = chat.Settings.Environment.IsDefaultWorkingDirectoryEnabled,
						IsActive = chat.Settings.Environment.IsDefaultWorkingDirectoryActive,
						Path = Directories.DefaultWorkingDirectory
					})
					.Where(wd => wd.IsEnabled && string.IsNullOrEmpty(wd.Path))
					.Select(wd => wd.Path)
					.ToArray()!;
			else
				projectPaths = [chat.Settings.Environment.GetWorkingDirectory()];

			List<string> projectCheckPaths = [
				$"{Directories.WorkingHome}/skills",
				".agents/skills",
				".claude/skills",
				".github/skills",
				".codex/skills",
				".cursor/skills"
			];
			List<string> potentialSkillDirectories = [];
			foreach (string projectPath in projectPaths)
			{
				foreach (string checkPath in projectCheckPaths)
				{
					var potentialSkillDirectory = Path.Combine(projectPath, checkPath);
					potentialSkillDirectories.Add(potentialSkillDirectory);
				}
			}

			potentialSkillDirectories.Add(Directories.Skills);
			potentialSkillDirectories.AddRange(chat.Settings.Skills.AdditionalSkillDirectories);

			List<string> skillFiles = [];

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

			skillFiles.AddRange(chat.Settings.Skills.AdditionalSkillFiles);

			return skillFiles.Where(File.Exists);
		}
	}
}
