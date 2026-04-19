using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LLMDesktopAssistant.Prompting
{
	public partial class PromptManagerView : UserControl
	{
		public PromptManagerView()
		{
			InitializeComponent();
		}

		private void InitializeComponent()
		{
			AvaloniaXamlLoader.Load(this);
		}
	}
}