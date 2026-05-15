using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.WebUI
{
	public static class WebUIStaticConfiguration
	{
		public const string MasterCookiesScheme = "MasterCookies";
		public const string LoginCookiesScheme = "LoginCookies";

		public static TimeSpan DisconnectTimeout { get; } = TimeSpan.FromMinutes(2);
		public static TimeSpan MasterExpiryTimeSpan { get; } = TimeSpan.FromHours(24);
		public static TimeSpan LoginExpiryTimeSpan { get; } = TimeSpan.FromHours(24);
	}
}