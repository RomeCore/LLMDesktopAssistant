using LLMDesktopAssistant.Settings;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.Agents.Settings
{
	public class AgentsConfiguration : SettingsObject
	{
		private RangeObservableCollection<ChatAgentDescriptor> _agents = [];
		public RangeObservableCollection<ChatAgentDescriptor> Agents
		{
			get => _agents;
			set => _agents.Reset(value);
		}
	}
}