using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.WebUI
{
	public static class WebUIStaticConfiguration
	{
		public const string CookiesScheme = "WebUICookies";
		public const string LoginClaim = "dASS/webui/Login";
		public const string MasterPasswordClaim = "dASS/webui/MasterPassword";
		public const string PasswordClaim = "dASS/webui/Password";

		public static TimeSpan DisconnectTimeout { get; } = TimeSpan.FromMinutes(2);
		public static TimeSpan LoginExpiryTimeSpan { get; } = TimeSpan.FromHours(24);
	}
}