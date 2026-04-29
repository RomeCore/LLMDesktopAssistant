using System;
using System.Collections.Generic;
using System.Text;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// The descriptor for an agent, containing all the necessary information to configure and execute it.
	/// </summary>
	public class AgentDescriptor : NotifyPropertyChanged
	{
		private Guid _id = Guid.NewGuid();
		/// <summary>
		/// Unique identifier for this agent.
		/// </summary>
		public Guid Id
		{
			get => _id;
			set => SetProperty(ref _id, value);
		}

		private AgentExecutionConditionsSettings _executionConditionsSettings = new();
		/// <summary>
		/// The settings that determine when and how the agent should execute.
		/// </summary>
		public AgentExecutionConditionsSettings ExecutionConditions
		{
			get => _executionConditionsSettings;
			set => SetProperty(ref _executionConditionsSettings, value);
		}

		private AgentGenerationSettings _generationSettings = new();
		/// <summary>
		/// The generation settings for the agent.
		/// </summary>
		public AgentGenerationSettings Generation
		{
			get => _generationSettings;
			set => SetProperty(ref _generationSettings, value);
		}

		private AgentReadSettings _readSettings = new();
		/// <summary>
		/// The settings that determine what the agent can read.
		/// </summary>
		public AgentReadSettings Read
		{
			get => _readSettings;
			set => SetProperty(ref _readSettings, value);
		}

		private AgentPromptSettings _promptSettings = new();
		/// <summary>
		/// The prompt settings for this agent.
		/// </summary>
		public AgentPromptSettings Prompts
		{
			get => _promptSettings;
			set => SetProperty(ref _promptSettings, value);
		}

		private AgentToolSettings _toolSettings = new();
		/// <summary>
		/// The tool settings for this agent.
		/// </summary>
		public AgentToolSettings Tools
		{
			get => _toolSettings;
			set => SetProperty(ref _toolSettings, value);
		}
	}
}