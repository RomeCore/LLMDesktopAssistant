using LLMDesktopAssistant.LLM.Settings;
using LLMDesktopAssistant.Localization;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Addons.Loading
{
	/// <summary>
	/// The locator that find only fixed implicit packs that are not configurable.
	/// Configurable packs are located by <see cref="IChatAddonPackLocator"/>.
	/// </summary>
	[Service(typeof(IAddonPackLocator))]
	[Service(typeof(IAppAddonPackLocator))]
	public class AppAddonPackLocator(
		IAddonPackSearchFoldersProvider foldersProvider
	) : AddonPackLocatorBase, IAppAddonPackLocator
	{
		public override IEnumerable<AddonPackInfo> GetAllPacks()
		{
			var result = new List<AddonPackInfo>
			{
				new()
				{
					Name = "%LOCALAPPDATA%",
					Path = Directories.LocalAppData,
					Source = AddonPackSource.AppData,
					IsConfigurable = false,

					NameKey = Locale.GetKey("addon.pack.localappdata.name"),
					DescriptionKey = Locale.GetKey("addon.pack.localappdata.description")
				}
			};

			var sharedRootFolder = Directories.UserProfile;
			var searchFolders = foldersProvider.GetSearchFolders().ToArray();

			foreach (var folder in searchFolders)
			{
				var combinedPath = Path.Combine(sharedRootFolder, folder);
				if (Directory.Exists(combinedPath))
				{
					result.Add(new AddonPackInfo
					{
						Name = folder,
						Path = combinedPath,
						Source = AddonPackSource.UserAgentsHome,
						IsConfigurable = false,

						DescriptionKey = Locale.GetKey("addon.pack.useragentshome.description")
					});
				}
			}

			return result;
		}

		protected override AddonPacksSettings? GetEffectiveSettings()
		{
			return null;
		}
	}
}
