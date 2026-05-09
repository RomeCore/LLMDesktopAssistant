using LLMDesktopAssistant.Settings;
using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.WebUI
{
	public class WebUIStartupSettings : SettingsObject
	{
		private string _endpointUrl = "http://localhost:5000";
		/// <summary>
		/// The URL of the endpoint where the Web UI is hosted.
		/// </summary>
		public string EndpointUrl
		{
			get => _endpointUrl;
			set => SetProperty(ref _endpointUrl, value);
		}

		private string? _passwordHash;
		/// <summary>
		/// The optional password hash used to enter the Web UI.
		/// When not null or empty, users must provide a password that matches this hash to access the Web Chat UI.
		/// </summary>
		public string? PasswordHash
		{
			get => _passwordHash;
			set => SetProperty(ref _passwordHash, value);
		}
	}
}