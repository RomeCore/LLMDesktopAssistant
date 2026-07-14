using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Users
{
	/// <summary>
	/// Configuration for remote users that funtions as database for remote users that connects to Blazor WebUI.
	/// </summary>
	public class RemoteUsersConfiguration : SettingsObject
	{
		private RangeObservableCollection<UserInformation> _users = [];
		public RangeObservableCollection<UserInformation> Users
		{
			get => _users;
			set => _users.Reset(value);
		}
	}
}