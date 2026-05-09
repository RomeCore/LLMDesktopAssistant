using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.WebUI
{
	public interface IChatWebUIStarter
	{
		public bool IsRunning { get; }

		public void Start(WebUIStartupSettings settings);
		public void Stop();
	}
}
