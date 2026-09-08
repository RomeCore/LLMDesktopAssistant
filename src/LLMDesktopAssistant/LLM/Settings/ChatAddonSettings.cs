using LLMDesktopAssistant.SourceGenerators;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Settings related to language models used in chat.
	/// </summary>
	[SettingsRoute(nameof(ChatSettings.Addons))]
	public partial class ChatAddonSettings : ChatSettingsCategoryBase
	{
		/// <summary>
		/// The additional search folders for addons, similar to plain '.agents', '.claude'.
		/// </summary>
		/// <remarks>
		/// IMPORTANT: This setting is only effective at APP-level inheritance.
		/// Access this setting using <c>ApplicationSettingsAccessor.ApplicationSettings.InheritedChatSettings.Addons</c>.
		/// </remarks>
		public RangeObservableCollection<string> AdditionalSearchFolders
		{
			get => field ??= [];
			set => (field ??= []).Reset(value);
		}

		private AddonWorkingDirectoriesSettings _workingDirectories = new();
		/// <summary>
		/// The working directories group: whether to fetch addons from all enabled working
		/// directories and whether to use working directories as packs.
		/// </summary>
		[InheritedChatSetting]
		public AddonWorkingDirectoriesSettings WorkingDirectories
		{
			get => _workingDirectories;
			set => SetProperty(ref _workingDirectories, value);
		}

		/// <summary>
		/// The additional pack paths for addons, similar to '~/.agents/packs/my-pack/'.
		/// </summary>
		[InheritedChatSetting]
		public RangeObservableCollection<string> AdditionalPackPaths
		{
			get => field ??= [];
			set => (field ??= []).Reset(value);
		}

		/// <summary>
		/// The settings for additional addon sources per addon type.
		/// </summary>
		[InheritedChatSetting]
		public ObservableDictionary<string, AddonSourcesSettings> AddonSources
		{
			get => field ??= [];
			set => (field ??= []).Reset(value);
		}

		/// <summary>
		/// The settings for addon packs.
		/// </summary>
		[InheritedChatSetting]
		public AddonPacksSettings Packs
		{
			get => field ??= new();
			set => SetProperty(ref field, value);
		}
	}
}
