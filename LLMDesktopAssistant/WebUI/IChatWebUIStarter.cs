using System.ComponentModel;

namespace LLMDesktopAssistant.WebUI
{
	public interface IChatWebUIStarter : INotifyPropertyChanged
	{
		public bool IsRunning { get; }

		public void Start(WebUIStartupSettings settings);
		public void Stop();
	}
}
