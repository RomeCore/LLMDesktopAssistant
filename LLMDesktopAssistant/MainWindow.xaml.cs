using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LLMDesktopAssistant.Core.Modules;
using LLMDesktopAssistant.Core.Modules.Instances;
using LLMDesktopAssistant.Core.MVVM;
using LLMDesktopAssistant.Core.Speech;
using MahApps.Metro.Controls;
using Microsoft.Web.WebView2.Core;

namespace LLMDesktopAssistant.Core
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : MetroWindow
	{
		public MainWindow()
		{
			InitializeComponent();

			ContentTemplateSelector = ViewLocator.Instance;
			Content = new MainViewModel();

			var themeModule = ModuleManager.Get<ThemeModule>();
			themeModule.ThemeType = ThemeType.Dark;
			themeModule.PrimaryColor = Color.FromRgb(255, 52, 12);
			themeModule.SecondaryColor = Color.FromRgb(76, 175, 80);
		}
	}
}