using Avalonia.Controls;
using LLMDesktopAssistant.Controls.Toasts;
using LLMDesktopAssistant.Services;
using LLMDesktopAssistant.Services.Instances;

namespace LLMDesktopAssistant.MVVM
{
	public partial class MainView : UserControl
	{
		public MainView()
		{
			InitializeComponent();

			var toastService = ServiceRegistry.TryGet<ToastService>();
			if (toastService != null)
			{
				toastService.ToastControl = ToastControl;

				Task.Run(async () =>
				{
					await Task.Delay(3000);
					toastService.ShowSuccess("Test", "This is a test message.");
					await Task.Delay(2000);
					toastService.ShowInfo("Test Info", "This is an info message.");
					await Task.Delay(2000);
					toastService.ShowWarning("Test Warning", "This is a warning message.");
					await Task.Delay(2000);
					toastService.ShowError("Test Error", "This is an error message.");
				});
			}
		}
	}
}