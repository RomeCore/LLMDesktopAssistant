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

			var toastService = ServiceRegistry.Get<ToastService>();
			if (toastService != null)
			{
				toastService.ToastControl = ToastControl;
			}
		}
	}
}