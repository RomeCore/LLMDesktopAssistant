using System.Text.Json.Serialization;

namespace LLMDesktopAssistant.Settings
{
	public class SettingsObject : NotifyPropertyChanged
	{
		/// <summary>
		/// The default ID used for settings instances without an explicit identifier.
		/// </summary>
		public const string DefaultId = "-default";

		/// <summary>
		/// Gets the current ID of this settings instance.
		/// </summary>
		[JsonIgnore]
		public string Id
		{
			get;
			set => SetProperty(ref field, value);
		} = DefaultId;
	}
}