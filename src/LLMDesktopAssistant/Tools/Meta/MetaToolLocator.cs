using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Tools.Meta
{
	/// <summary>
	/// The default <see cref="IMetaToolLocator"/> implementation that scans the application-wide
	/// meta tools directory, the configured additional directories and files, and the working
	/// directory meta tools folders.
	/// </summary>
	[ChatService(typeof(IMetaToolLocator))]
	public class MetaToolLocator(
		IChatSettingsService chatSettings,
		IEnumerable<IMetaToolEngine> engines
	) : IMetaToolLocator
	{
		/// <inheritdoc/>
		public IEnumerable<MetaToolFileInfo> LocateMetaToolFiles()
		{
			var extensions = engines
				.SelectMany(e => e.Descriptor.Extensions)
				.ToHashSet(StringComparer.OrdinalIgnoreCase);

			List<MetaToolFileInfo> metaToolFiles = [];

			// 1. Application-wide meta tools directory
			metaToolFiles.AddRange(Directory.GetFiles(Directories.Metatools)
				.Select(f => new MetaToolFileInfo(f, MetaToolSource.UserProfile)));

			// 2. Additional directories and files
			var sources = chatSettings.Settings.Tools.GetEffectiveSources();

			foreach (var directory in sources.AdditionalMetaToolDirectories)
			{
				if (Directory.Exists(directory))
				{
					metaToolFiles.AddRange(Directory.GetFiles(directory)
						.Select(f => new MetaToolFileInfo(f, MetaToolSource.Custom)));
				}
			}

			metaToolFiles.AddRange(sources.AdditionalMetaToolFiles
				.Select(f => new MetaToolFileInfo(f, MetaToolSource.Custom)));

			// 3. Working directory meta tools folders
			string[] projectPaths;
			if (sources.FetchFromAllWorkingDirectories)
				projectPaths = [..chatSettings.Settings.Environment.GetEffectiveWorkingDirectories().GetEnabledWorkingDirectories()];
			else
				projectPaths = [chatSettings.Settings.Environment.GetEffectiveWorkingDirectories().GetWorkingDirectory()];

			foreach (var projectPath in projectPaths)
			{
				var metaToolsDir = Path.Combine(projectPath, Directories.WorkingHome, "metatools");
				if (Directory.Exists(metaToolsDir))
				{
					metaToolFiles.AddRange(Directory.GetFiles(metaToolsDir)
						.Select(f => new MetaToolFileInfo(f, MetaToolSource.WorkingDirectory)));
				}
			}

			var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
			return metaToolFiles
				.Where(f => extensions.Contains(Path.GetExtension(f.FileName)))
				.DistinctBy(f => f.FileName, comparer)
				.Where(f => File.Exists(f.FileName));
		}
	}
}
