using LLMDesktopAssistant.Settings;

namespace LLMDesktopAssistant.Users
{
	public class LocalUserConfiguration : SettingsObject
	{
		private bool _isLocallyOnline;
		/// <summary>
		/// Gets or sets a value indicating whether the local user is online.
		/// </summary>
		public bool IsLocallyOnline
		{
			get => _isLocallyOnline;
			set => SetProperty(ref _isLocallyOnline, value);
		}
	}
}