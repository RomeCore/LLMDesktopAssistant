using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LLMDesktopAssistant.LLM.Settings
{
    public partial class ChatPromptSettingsView : UserControl
    {
        public ChatPromptSettingsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}