using LLMDesktopAssistant.Prompting.Skills;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	[ChatService(typeof(ISkillLocator))]
	public class SkillLocator(
		IChatSettingsService chatSettings
	) : ISkillLocator
	{
		public IEnumerable<SkillFileInfo> LocateSkillFiles()
		{
			// 1. Skill directories
			List<SkillFileInfo> potentialSkillDirectories = [];

			potentialSkillDirectories.Add(new SkillFileInfo(Directories.Skills, SkillSource.UserProfile));

			var skillSources = chatSettings.Settings.Skills.GetEffectiveSources();
			potentialSkillDirectories.AddRange(skillSources.AdditionalSkillDirectories.Select(d => new SkillFileInfo(d, SkillSource.Custom)));

			string[] projectPaths;
			if (skillSources.FetchFromAllWorkingDirectories)
				projectPaths = [..chatSettings.Settings.Environment.GetEffectiveWorkingDirectories().GetEnabledWorkingDirectories()];
			else
				projectPaths = [chatSettings.Settings.Environment.GetEffectiveWorkingDirectories().GetWorkingDirectory()];

			List<string[]> sharedCheckPaths = [
				[Directories.WorkingHome, "skills"], // .llmassist/skills
				[".agents", "skills"],
				[".claude", "skills"],
				[".github", "skills"],
				[".codex", "skills"],
				[".gemini", "skills"],
				[".cursor", "skills"]
			];

			var sharedRootFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			foreach (var sharedCheckPath in sharedCheckPaths)
			{
				var potentialSkillDirectory = Path.Combine([sharedRootFolder, ..sharedCheckPath]);
				potentialSkillDirectories.Add(new SkillFileInfo(potentialSkillDirectory, SkillSource.UserProfile));
			}

			List<string[]> projectCheckPaths = [];
			projectCheckPaths.AddRange(sharedCheckPaths);
			foreach (string projectPath in projectPaths)
			{
				foreach (var checkPath in projectCheckPaths)
				{
					var potentialSkillDirectory = Path.Combine([projectPath, ..checkPath]);
					potentialSkillDirectories.Add(new SkillFileInfo(potentialSkillDirectory, SkillSource.WorkingDirectory));
				}
			}

			// 2. Skill files

			List<SkillFileInfo> skillFiles = [];

			skillFiles.AddRange(skillSources.AdditionalSkillFiles.Select(f => new SkillFileInfo(f, SkillSource.Custom)));

			foreach (var potentialSkillDirectory in potentialSkillDirectories)
			{
				if (!Directory.Exists(potentialSkillDirectory.FileName))
					continue;

				var subdirectories = Directory.GetDirectories(potentialSkillDirectory.FileName);
				foreach (string subdirectory in subdirectories)
				{
					skillFiles.Add(new SkillFileInfo(Path.Combine(subdirectory, "SKILL.md"), potentialSkillDirectory.Source));
					skillFiles.Add(new SkillFileInfo(Path.Combine(subdirectory, "SKILL.mdx"), potentialSkillDirectory.Source));
				}
			}

			var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
			return skillFiles.DistinctBy(s => s.FileName, comparer).Where(s => File.Exists(s.FileName));
		}
	}
}
