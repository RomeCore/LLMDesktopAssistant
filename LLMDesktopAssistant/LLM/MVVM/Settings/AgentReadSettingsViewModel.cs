using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.LLM.Settings;

namespace LLMDesktopAssistant.LLM.MVVM.Settings
{
	[ViewModelFor(typeof(AgentReadSettingsView))]
	public class AgentReadSettingsViewModel : ViewModelBase
	{
		public AgentReadSettings ReadSettings { get; }

		public AgentReadSettingsViewModel(AgentReadSettings settings)
		{
			ReadSettings = settings;
		}
	}
}
