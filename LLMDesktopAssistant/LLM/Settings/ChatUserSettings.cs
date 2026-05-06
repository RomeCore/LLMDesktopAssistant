using LLMDesktopAssistant.LLM.Domain;
using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	public class ChatUserSettings : SettingsObject
	{
		private readonly RangeObservableCollection<UserInformation> _users = [ new UserInformation
		{
			Login = "user",
			Name = "User"
		} ];
		/// <summary>
		/// List of local users that can interact with the chat.
		/// </summary>
		public RangeObservableCollection<UserInformation> Users
		{
			get => _users;
			set => _users.Reset(value);
		}
	}
}