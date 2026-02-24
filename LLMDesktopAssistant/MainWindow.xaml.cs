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
using LLMDesktopAssistant.Modules;
using LLMDesktopAssistant.MVVM;
using LLMDesktopAssistant.Speech;
using Microsoft.Web.WebView2.Core;

namespace LLMDesktopAssistant
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			var speechProvider = ModuleManager.GetDynamic<ISpeechProvider>();
			speechProvider.OnSpeechReceived += speech =>
			{
				Console.WriteLine(speech);
			};
		}
	}
}