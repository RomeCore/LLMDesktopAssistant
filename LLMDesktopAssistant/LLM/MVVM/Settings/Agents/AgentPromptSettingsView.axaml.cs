using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents
{
    public partial class AgentPromptSettingsView : UserControl
    {
        public AgentPromptSettingsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}