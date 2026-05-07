using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.LLM.Services
{
	public interface IChatBlazorUIStarter
	{
		public bool IsRunning { get; }

		public void Start();
		public void Stop();
	}
}
