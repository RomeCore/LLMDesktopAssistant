using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace LLMDesktopAssistant.Agents
{
	/// <summary>
	/// The descriptor for an agent, containing all the necessary information to configure and execute it.
	/// </summary>
	public class ChatAgentDescriptor : NotifyPropertyChanged
	{
		private static readonly JsonSerializerOptions _cloneOptions = new JsonSerializerOptions
		{
			WriteIndented = false,
			DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
			IncludeFields = true,
			PropertyNameCaseInsensitive = true,
		};

		private Guid _id = Guid.NewGuid();
		/// <summary>
		/// Unique identifier for this agent.
		/// </summary>
		public Guid Id
		{
			get => _id;
			set => SetProperty(ref _id, value);
		}

		private AgentInformation _info = new();
		/// <summary>
		/// Information about the agent, such as its name and profile image.
		/// </summary>
		public AgentInformation Info
		{
			get => _info;
			set => SetProperty(ref _info, value);
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

		/// <summary>
		/// Creates a deep clone of this agent descriptor via JSON serialization.
		/// </summary>
		public ChatAgentDescriptor Clone()
		{
			var json = JsonSerializer.Serialize(this, _cloneOptions);
			var clone = JsonSerializer.Deserialize<ChatAgentDescriptor>(json, _cloneOptions);
			clone!._id = Guid.NewGuid();
			return clone;
		}
	}
}
