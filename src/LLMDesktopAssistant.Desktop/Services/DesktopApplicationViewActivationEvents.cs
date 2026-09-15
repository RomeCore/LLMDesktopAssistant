using Avalonia.Controls;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;

namespace LLMDesktopAssistant.Desktop.Services
{
	[Service(typeof(IApplicationViewActivationEvents))]
	public class DesktopApplicationViewActivationEvents : IApplicationViewActivationEvents
	{
		public event Action? Activated;

		public DesktopApplicationViewActivationEvents()
		{
			App.OnFrameworkInitializationCompleted(() =>
			{
				var window = App.MainWindow!;
				window.Activated += (s, e) => Activated?.Invoke();
			});
		}
	}
}
