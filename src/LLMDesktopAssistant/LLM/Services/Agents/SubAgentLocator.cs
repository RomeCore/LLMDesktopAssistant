using AvaloniaEdit.Utils;
using LLMDesktopAssistant.Agents.SubAgents;
using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Services.Prompting
{
	[ChatService(typeof(ISubAgentLocator))]
	public class SubAgentLocator(
		IChatSettingsService chatSettings
	) : ISubAgentLocator
	{
		public IEnumerable<SubAgentFileInfo> LocateSubAgentFiles()
		{
			// 1. Sub-agent directories
			List<SubAgentFileInfo> potentialSubAgentDirectories = [];

			var subAgentSources = chatSettings.Settings.SubAgents.GetEffectiveSources();
			potentialSubAgentDirectories.AddRange(subAgentSources.AdditionalSubAgentDirectories.Select(d => new SubAgentFileInfo(d, SubAgentSource.Custom)));

			string[] projectPaths;
			if (subAgentSources.FetchFromAllWorkingDirectories)
				projectPaths = [..chatSettings.Settings.Environment.GetEffectiveWorkingDirectories().GetEnabledWorkingDirectories()];
			else
				projectPaths = [chatSettings.Settings.Environment.GetEffectiveWorkingDirectories().GetWorkingDirectory()];

			List<string[]> sharedCheckPaths = [
				[Directories.WorkingHome, "agents"], // .llmassist/agents
				[".agents", "agents"],
				[".claude", "agents"],
				[".github", "agents"],
				[".codex", "agents"],
				[".gemini", "agents"],
				[".cursor", "agents"]
			];

			var sharedRootFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			foreach (var sharedCheckPath in sharedCheckPaths)
			{
				var potentialSubAgentDirectory = Path.Combine([sharedRootFolder, ..sharedCheckPath]);
				potentialSubAgentDirectories.Add(new SubAgentFileInfo(potentialSubAgentDirectory, SubAgentSource.UserProfile));
			}

			List<string[]> projectCheckPaths = [];
			projectCheckPaths.AddRange(sharedCheckPaths);
			foreach (string projectPath in projectPaths)
			{
				foreach (var checkPath in projectCheckPaths)
				{
					var potentialSubAgentDirectory = Path.Combine([projectPath, ..checkPath]);
					potentialSubAgentDirectories.Add(new SubAgentFileInfo(potentialSubAgentDirectory, SubAgentSource.WorkingDirectory));
				}
			}

			// 2. Sub-agent files

			List<SubAgentFileInfo> subAgentFiles = [];

			subAgentFiles.AddRange(subAgentSources.AdditionalSubAgentFiles.Select(f => new SubAgentFileInfo(f, SubAgentSource.Custom)));

			foreach (var potentialSubAgentDirectory in potentialSubAgentDirectories)
			{
				if (!Directory.Exists(potentialSubAgentDirectory.FileName))
					continue;

				var files = Directory.GetFiles(potentialSubAgentDirectory.FileName, "*.md")
					.Concat(Directory.GetFiles(potentialSubAgentDirectory.FileName, "*.mdx"));
				foreach (string file in files)
				{
					subAgentFiles.Add(new SubAgentFileInfo(file, potentialSubAgentDirectory.Source));
				}
			}

			var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
			return subAgentFiles.DistinctBy(s => s.FileName, comparer).Where(s => File.Exists(s.FileName));
		}
	}
}
