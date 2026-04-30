using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	public class ChatAgentSettings : NotifyPropertyChanged
	{
		private RangeObservableCollection<AgentDescriptor> _agents = [];
		/// <summary>
		/// The list of chat-specific agents (local to this chat session).
		/// </summary>
		public RangeObservableCollection<AgentDescriptor> ChatAgents
		{
			get => _agents;
			set => _agents.Reset(value);
		}

		private RangeObservableCollection<AgentInstanceSettings> _activeAgents = [];
		/// <summary>
		/// The list of active agent instances in the chat session.
		/// ActiveAgents defines the ordered execution list and which agents are enabled.
		/// </summary>
		public RangeObservableCollection<AgentInstanceSettings> ActiveAgents
		{
			get => _activeAgents;
			set => _activeAgents.Reset(value);
		}

		/// <summary>
		/// Ensures that at least one agent exists and is active.
		/// Creates a default agent if none exist.
		/// </summary>
		public void EnsureDefaultAgent()
		{
			// If no agents at all, create a default one
			if (ChatAgents.Count == 0)
			{
				var defaultAgent = new AgentDescriptor();
				defaultAgent.Info.Name = "Default Assistant";
				ChatAgents.Add(defaultAgent);
			}

			// If no active agents, activate the first one
			if (ActiveAgents.Count == 0 && ChatAgents.Count > 0)
			{
				ActiveAgents.Add(new AgentInstanceSettings
				{
					AgentId = ChatAgents[0].Id,
					Enabled = true
				});
			}
		}
	}
}
