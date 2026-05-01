using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace LLMDesktopAssistant.LLM.MVVM.Settings.Agents
{
    public partial class AgentInfoSettingsView : UserControl
    {
        public AgentInfoSettingsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
