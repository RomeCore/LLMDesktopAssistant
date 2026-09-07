using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Settings.Application;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Addons.Loading
{
	[Service(typeof(IAddonPackSearchFoldersProvider))]
	public class AddonPackSearchFoldersProvider : IAddonPackSearchFoldersProvider
	{
		private static readonly ImmutableArray<string> defaultSearchFolders;

		static AddonPackSearchFoldersProvider()
		{
			defaultSearchFolders = [
				Directories.WorkingHome, // .llmassist
				".agents",
				".claude",
				".github",
				".codex",
				".gemini",
				".cursor",
				".claw",
				".everywhere" // OLOLOLO MY COMPETITORRR
			];
		}

		public IEnumerable<string> GetDefaultSearchFolders()
		{
			return defaultSearchFolders;
		}

		public IEnumerable<string> GetSearchFolders()
		{
			HashSet<string> result = [..defaultSearchFolders];

			var appconfig = ApplicationSettingsAccessor.ApplicationSettings.InheritedChatSettings.Addons.AdditionalSearchFolders;

			foreach (var folder in appconfig)
				result.Add(folder);

			return result;
		}
	}
}
