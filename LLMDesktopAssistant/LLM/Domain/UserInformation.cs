using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.LLM.Domain
{
	public class UserInformation : NotifyPropertyChanged
	{
		private string _login = string.Empty;
		/// <summary>
		/// The login of the user.
		/// </summary>
		public string Login
		{
			get => _login;
			set => SetProperty(ref _login, value);
		}

		private string _name = string.Empty;
		/// <summary>
		/// The name of the user.
		/// </summary>
		public string Name
		{
			get => _name;
			set => SetProperty(ref _name, value);
		}

		private string _description = string.Empty;
		/// <summary>
		/// The description of the user, contains additional information for agents.
		/// </summary>
		public string Description
		{
			get => _description;
			set => SetProperty(ref _description, value);
		}

		private string _base64ProfileImage = string.Empty;
		/// <summary>
		/// The base64 encoded profile image of the user. If empty, no image is displayed.
		/// </summary>
		public string Base64ProfileImage
		{
			get => _base64ProfileImage;
			set => SetProperty(ref _base64ProfileImage, value);
		}
	}
}