using LLMDesktopAssistant.SourceGenerators;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	/// <summary>
	/// Environment and working directory settings.
	/// </summary>
	[SettingsRoute(nameof(ChatSettings.Environment))]
	public partial class ChatEnvironmentSettings : ChatSettingsCategoryBase
	{
		private WorkingDirectoriesSettings _workingDirectories = new();
		/// <summary>
		/// Gets or sets the working directory configuration for the chat.
		/// </summary>
		[InheritedChatSetting]
		public WorkingDirectoriesSettings WorkingDirectories
		{
			get => _workingDirectories;
			set => SetProperty(ref _workingDirectories, value);
		}
		
		private readonly RangeObservableCollection<DirectoryAccessSetting> _directoryAccessRules = [];
		/// <summary>
		/// The list of directory access rules.
		/// </summary>
		[InheritedChatSetting]
		public RangeObservableCollection<DirectoryAccessSetting> DirectoryAccessRules
		{
			get => _directoryAccessRules;
			set => _directoryAccessRules.Reset(value);
		}

		private readonly RangeObservableCollection<AdditionalEnvironmentSetting> _additionalSettings = [];
		/// <summary>
		/// The list of additional environment settings.
		/// </summary>
		[InheritedChatSetting]
		public RangeObservableCollection<AdditionalEnvironmentSetting> AdditionalSettings
		{
			get => _additionalSettings;
			set => _additionalSettings.Reset(value);
		}

		/// <summary>
		/// Returns an additional environment setting of type <typeparamref name="T"/>. If no such setting exists, a new one is created and added to the collection.
		/// </summary>
		/// <typeparam name="T">The type of the additional environment setting. Must inherit from <see cref="AdditionalEnvironmentSetting"/> and have a parameterless constructor.</typeparam>
		/// <returns>The additional environment setting of type <typeparamref name="T"/>.</returns>
		public T EnsureAdditional<T>() where T : AdditionalEnvironmentSetting, new()
		{
			if (AdditionalSettings.FirstOrDefault(s => s is T) is T found) return found;
			found = new();
			AdditionalSettings.Add(found);
			return found;
		}
	}
}
