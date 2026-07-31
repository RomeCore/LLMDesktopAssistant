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

			potentialSkillDirectories.Add(Directories.Skills);
			potentialSkillDirectories.AddRange(chat.Settings.Skills.AdditionalSkillDirectories);

			string[] projectPaths;
			if (chat.Settings.Skills.FetchFromAllWorkingDirectories)
				projectPaths = chat.Settings.Environment.WorkingDirectories
					.Append(new WorkingDirectorySetting
					{
						IsEnabled = chat.Settings.Environment.IsDefaultWorkingDirectoryEnabled,
						IsActive = chat.Settings.Environment.IsDefaultWorkingDirectoryActive,
						Path = Directories.DefaultWorkingDirectory
					})
					.Where(wd => wd.IsEnabled && !string.IsNullOrEmpty(wd.Path))
					.Select(wd => wd.Path)
					.ToArray()!;
			else
				projectPaths = [chat.Settings.Environment.GetWorkingDirectory()];

			List<string> sharedCheckPaths = [
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

			List<string> projectCheckPaths = [
				$"{Directories.WorkingHome}/skills"
			];
			foreach (string projectPath in projectPaths)
			{
				foreach (string checkPath in projectCheckPaths.Concat(sharedCheckPaths))
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
