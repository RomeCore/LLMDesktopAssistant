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
		/// <summary>
		/// Gets or sets the working directory configuration for the chat.
		/// </summary>
		[InheritedChatSetting]
		public WorkingDirectoriesSettings WorkingDirectories
		{
			get => field ??= new();
			set => SetProperty(ref field, value);
		}
		
		/// <summary>
		/// The list of directory access rules.
		/// </summary>
		[InheritedChatSetting]
		public RangeObservableCollection<DirectoryAccessSetting> DirectoryAccessRules
		{
			get => field ??= new();
			set => (field ??= new()).Reset(value);
		}

		/// <summary>
		/// The list of additional environment settings.
		/// </summary>
		[InheritedChatSetting]
		public RangeObservableCollection<AdditionalEnvironmentSetting> AdditionalSettings
		{
			get => field ??= new();
			set => (field ??= new()).Reset(value);
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
