using LLMDesktopAssistant.LLM.Services;
using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Addons.Loading
{
	/// <summary>
	/// The locator that finds both configurable packs (even user-scoped) and workspace-scoped packs.
	/// </summary>
	[ChatService(typeof(IChatAddonPackLocator))]
	public class ChatAddonPackLocator(
		IAddonPackSearchFoldersProvider foldersProvider,
		IChatSettingsService chatSettings
	) : AddonPackLocatorBase, IChatAddonPackLocator
	{
		public override IEnumerable<AddonPackInfo> GetAllPacks()
		{
			var result = new List<AddonPackInfo>();

			foreach (var pack in Directory.GetDirectories(Directories.AddonPacks))
			{
				result.Add(ParsePack(pack, AddonPackSource.Scanned));
			}

			var sharedRootFolder = Directories.UserProfile;
			var searchFolders = foldersProvider.GetSearchFolders().ToArray();

			foreach (var folder in searchFolders)
			{
				var combinedPacksPath = Path.Combine(sharedRootFolder, folder, "packs");
				if (Directory.Exists(combinedPacksPath))
				{
					foreach (var pack in Directory.GetDirectories(combinedPacksPath))
					{
						result.Add(ParsePack(pack, AddonPackSource.Scanned));
					}
				}
			}

			var workdirSettings = chatSettings.Settings.Environment.GetEffectiveWorkingDirectories();
			var fetchFromAllWorkdirs = chatSettings.Settings.Addons.GetEffectiveFetchFromAllWorkingDirectories();

			var workingDirectories = fetchFromAllWorkdirs
				? workdirSettings.GetEnabledWorkingDirectories()
				: [workdirSettings.GetWorkingDirectory()];

			foreach (var workdir in workingDirectories)
			{
				foreach (var folder in searchFolders)
				{
					var combinedPath = Path.Combine(workdir, folder);
					if (Directory.Exists(combinedPath))
					{
						result.Add(new AddonPackInfo
						{
							Name = folder,
							Path = combinedPath,
							Source = AddonPackSource.AgentsHome,

							NameKey = Locale.GetKey("addon.pack.agentshome.name"),
							DescriptionKey = Locale.GetKey("addon.pack.agentshome.description")
						});
					}

					var combinedPacksPath = Path.Combine(combinedPath, "packs");
					if (Directory.Exists(combinedPacksPath))
					{
						foreach (var pack in Directory.GetDirectories(combinedPacksPath))
						{
							result.Add(ParsePack(pack, AddonPackSource.Scanned));
						}
					}
				}
			}

			foreach (var additionalPack in chatSettings.Settings.Addons.GetEffectiveAdditionalPackPaths())
			{
				if (Directory.Exists(additionalPack))
				{
					result.Add(ParsePack(additionalPack, AddonPackSource.Configuration));
				}
			}

			return result;
		}

		protected override bool IsPackConfigurable(AddonPackInfo pack)
		{
			return pack.Source is AddonPackSource.Scanned or AddonPackSource.Configuration;
		}

		protected override AddonPacksSettings? GetEffectiveSettings()
		{
			return chatSettings.Settings.Addons.GetEffectivePacks();
		}
	}
}
