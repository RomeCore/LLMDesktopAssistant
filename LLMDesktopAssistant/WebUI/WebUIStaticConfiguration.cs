using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.WebUI
{
	public static class WebUIStaticConfiguration
	{
		public static string CookiesAuthScheme { get; } = "WebUICookies";

		public static TimeSpan AuthExpiryTimeSpan { get; } = TimeSpan.FromHours(24);
	}
}