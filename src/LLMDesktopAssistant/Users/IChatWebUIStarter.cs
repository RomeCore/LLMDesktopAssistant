using System.ComponentModel;

namespace LLMDesktopAssistant.Users
{
	public interface IChatWebUIStarter : INotifyPropertyChanged
	{
		public bool IsRunning { get; }

		public void Start(WebUIStartupSettings settings);
		public void Stop();
	}
}
