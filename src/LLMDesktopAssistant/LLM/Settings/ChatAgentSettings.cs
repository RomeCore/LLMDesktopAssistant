using LLMDesktopAssistant.Agents;
using LLMDesktopAssistant.Agents.ExecutionStages;
using LLMDesktopAssistant.Utils;

namespace LLMDesktopAssistant.LLM.Settings
{
	public class ChatAgentSettings : ChatSettingsCategoryBase
	{
		/// <summary>
		/// The list of chat-specific agents (local to this chat session).
		/// </summary>
		public RangeObservableCollection<ChatAgentDescriptor> ChatAgents
		{
			get => field ??= new();
			set => (field ??= new()).Reset(value);
		}

		/// <summary>
		/// The list of ordered agent execution stages for the chat session.
		/// </summary>
		public RangeObservableCollection<AgentExecutionStage> ExecutionStages
		{
			get => field ??= new();
			set => (field ??= new()).Reset(value);
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
				var defaultAgent = new ChatAgentDescriptor();
				defaultAgent.Info.Name = "Default Assistant";
				ChatAgents.Add(defaultAgent);
			}

			// Add default sequential execution stage if none exist
			if (ExecutionStages.Count == 0)
			{
				var sequentialStage = new SequentialAgentExecutionStage();
				sequentialStage.AgentInstances.Add(new ChatAgentInstance
				{
					AgentId = ChatAgents[0].Id,
					Enabled = true
				});
				ExecutionStages.Add(sequentialStage);
			}
		}
	}
}
