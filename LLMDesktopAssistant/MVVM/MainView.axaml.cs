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

			var toastService = ServiceRegistry.Provider.GetRequiredService<IToastService>();
			if (toastService is ToastService _toastService)
			{
				_toastService.ToastControl = ToastControl;
			}
		}
	}
}