using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	public class ChatAgentSettings : NotifyPropertyChanged
	{
		private RangeObservableCollection<AgentDescriptor> _agents = [];
		/// <summary>
		/// The list of chat-specific agents.
		/// </summary>
		public RangeObservableCollection<AgentDescriptor> ChatAgents
		{
			get => _agents;
			set => _agents.Reset(value);
		}

		private RangeObservableCollection<AgentInstanceSettings> _activeAgents = [];
		/// <summary>
		/// The list of active agents in the chat.
		/// </summary>
		public RangeObservableCollection<AgentInstanceSettings> ActiveAgents
		{
			get => _activeAgents;
			set => _activeAgents.Reset(value);
		}
	}
}