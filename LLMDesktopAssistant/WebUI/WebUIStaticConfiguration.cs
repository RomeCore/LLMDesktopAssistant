using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.WebUI
{
	public static class WebUIStaticConfiguration
	{
		public const string MasterCookiesScheme = "MasterCookies";
		public const string LoginCookiesScheme = "LoginCookies";

		public static TimeSpan AuthExpiryTimeSpan { get; } = TimeSpan.FromHours(24);
	}
}