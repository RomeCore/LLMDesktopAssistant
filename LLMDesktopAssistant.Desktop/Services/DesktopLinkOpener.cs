using System;
using System.Diagnostics;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;
using Serilog;

namespace LLMDesktopAssistant.Desktop.Services
{
	[Service(typeof(ILinkOpener))]
	public class DesktopLinkOpener : ILinkOpener
	{
		public void OpenLink(Uri uri)
		{
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = uri.AbsoluteUri,
					UseShellExecute = true
				});
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error opening link: {Link}", uri);
			}
		}
	}
}